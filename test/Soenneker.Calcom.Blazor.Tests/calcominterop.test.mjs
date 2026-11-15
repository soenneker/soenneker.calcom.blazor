import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

async function setup() {
    const listeners = new Set(), elements = new Map(), frames = [], calls = [];
    const frame = namespace => {
        const result = { tagName: 'IFRAME', name: `cal-embed=${namespace}`, src: 'https://app.cal.com/test', isConnected: true, contentWindow: {},
            closest: () => ({ remove: () => { result.isConnected = false; } }) };
        frames.push(result); return result;
    };
    const cal = (action, namespace) => {
        if (action !== 'init') return;
        cal.ns[namespace] ??= (method, args) => {
            calls.push({ namespace, method, args });
            if (method === 'inline') { args.elementOrSelector.frames.push(frame(namespace)); args.elementOrSelector.childNodes.push({}); }
            if (method === 'modal') frame(namespace);
            if (method === 'floatingButton') elements.set(args.attributes.id, { remove: () => elements.delete(args.attributes.id) });
            if (method === 'closeModal') frames.filter(f => f.name === `cal-embed=${namespace}`).forEach(f => f.isConnected = false);
        };
    };
    cal.ns = {}; cal.instance = {};
    const context = vm.createContext({ URL, console, setTimeout, clearTimeout, AbortController,
        MutationObserver: class { observe() {} disconnect() {} },
        window: { Cal: cal, addEventListener: (_, fn) => listeners.add(fn), removeEventListener: (_, fn) => listeners.delete(fn) },
        document: { body: {}, getElementById: id => elements.get(id), getElementsByName: name => frames.filter(frame => frame.name === name) }
    });
    const source = await readFile(new URL('../../src/Soenneker.Calcom.Blazor/wwwroot/js/calcominterop.js', import.meta.url), 'utf8');
    vm.runInContext(source.replaceAll('export function', 'function'), context);
    const received = [];
    let count = 0;
    const create = () => context.create(`test-${++count}`, JSON.stringify({ origin: 'https://app.cal.com' }), {
        invokeMethodAsync: (...args) => { received.push(args); return Promise.resolve(); }
    });
    const element = id => {
        const classes = new Set();
        const el = { frames: [], childNodes: [], classList: { contains: key => classes.has(key), remove: key => classes.delete(key) },
            replaceChildren() { this.frames.forEach(f => f.isConnected = false); this.frames = []; this.childNodes = []; } };
        elements.set(id, el); return el;
    };
    const emit = (frame, namespace, origin = 'https://app.cal.com', action = 'bookingSuccessfulV2') => {
        for (const callback of listeners) callback({ source: frame.contentWindow, origin, data: { fullType: `CAL:${namespace}:${action}`, data: { uid: 'booking-123' } } });
    };
    return { context, create, element, emit, received, listeners, calls, frames, elements };
}
const options = extra => JSON.stringify({ calLink: 'user/demo', ...extra });

test('isolates namespace events and validates frame source and origin', async () => {
    const env = await setup(), first = env.create(), second = env.create();
    const one = env.element('one'); env.element('two');
    first.createInline('one', options()); second.createInline('two', options());
    const namespace = env.calls[0].namespace;
    env.emit(one.frames[0], namespace, 'https://evil.test');
    env.emit({ contentWindow: {} }, namespace);
    env.emit(one.frames[0], 'unowned');
    assert.equal(env.received.length, 0);
    env.emit(one.frames[0], namespace);
    assert.equal(env.received.length, 1);
    assert.deepEqual(JSON.parse(env.received[0][2]), { namespace, elementId: 'one', data: { uid: 'booking-123' } });
    const old = one.frames[0]; first.destroyInline('one'); env.emit(old, namespace);
    assert.equal(env.received.length, 1);
    first.dispose(); second.dispose();
    assert.equal(env.listeners.size, 0);
});

test('preserves prefills and typed UI values, reuses namespaces, and protects containers', async () => {
    const env = await setup(), session = env.create(), other = env.create();
    const el = env.element('one');
    session.createInline('one', options({ config: { name: 'Jane', additionalFields: { name: 'Ignored', company: 'Example' } }, ui: { theme: 'dark', layout: 'week_view' } }));
    const first = env.calls[0];
    assert.equal(first.args.config.name, 'Jane');
    assert.equal(first.args.config.company, 'Example');
    assert.equal(first.args.config.layout, 'week_view');
    assert.equal(env.calls[1].args.theme, 'dark');
    assert.throws(() => other.createInline('one', options()), /another/);
    session.createInline('one', options());
    assert.equal(env.calls[2].namespace, first.namespace);
    assert.equal(el.frames.length, 1);
    session.updateInlineUi('one', JSON.stringify({ theme: 'light' }));
    assert.equal(env.calls.at(-1).args.theme, 'light');
    session.destroyInline('one');
    el.childNodes.push({});
    assert.throws(() => session.createInline('one', options()), /empty/);
    assert.throws(() => session.createInline('missing', options()), /not found/);
    assert.throws(() => session.updateInlineUi('missing', '{}'), /not been created/);
    session.dispose(); other.dispose();
});

test('cleans popup and floating button resources and supports repeat creation', async () => {
    const env = await setup(), session = env.create();
    await session.openPopup(options());
    assert.equal(env.frames.filter(f => f.isConnected).length, 1);
    await session.openPopup(options());
    assert.equal(env.frames.filter(f => f.isConnected).length, 1);
    session.createFloatingButton(options({ buttonText: 'Book', buttonPosition: 'bottom-left' }));
    assert.equal(env.elements.size, 1);
    session.destroyFloatingButton(); assert.equal(env.elements.size, 0);
    session.createFloatingButton(options());
    session.dispose(); session.dispose();
    assert.equal(env.elements.size, 0);
    assert.equal(env.frames.filter(f => f.isConnected).length, 0);
    assert.equal(env.listeners.size, 0);
    await assert.rejects(() => session.openPopup(options()), /disposed/);
});

test('rejects external links, traversal, SDK overrides, and mixed SDK origins', async () => {
    const env = await setup(), session = env.create();
    env.element('one');
    for (const calLink of ['https://evil.test/path', '//evil.test', '../other', 'user/%2e%2e/other', 'user/demo#hash'])
        assert.throws(() => session.createInline('one', options({ calLink })), /CalLink/);
    assert.throws(() => session.createInline('one', options({ config: { additionalFields: { iframeAttrs: 'bad' } } })), /reserved/);
    session.createInline('one', options({ calLink: 'user/demo?name=Jane%20Doe&redirect=https%3A%2F%2Fexample.com' }));
    env.context.prepare('https://app.cal.com/embed/embed.js');
    assert.throws(() => env.context.prepare('https://other.test/embed.js'), /Only one/);
    session.dispose();
});

test('bootstrap queues namespace initialization before the SDK arrives', async () => {
    const env = await setup();
    delete env.context.window.Cal;
    env.context.prepare('https://app.cal.com/embed/embed.js');
    env.context.window.Cal('init', 'demo', { origin: 'https://app.cal.com' });
    env.context.window.Cal.ns.demo('inline', { calLink: 'user/demo' });
    assert.equal(env.context.window.Cal.q[0][0], 'initNamespace');
    assert.equal(env.context.window.Cal.ns.demo.q.length, 2);
});
