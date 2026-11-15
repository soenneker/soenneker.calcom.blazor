let sdkUrl;
const containerOwners = new WeakMap();
// Cal's SDK has no namespace destructor. Reuse idle namespaces by origin and widget kind
// so Blazor navigation does not create an unbounded number of SDK instances.
const idleNamespaces = [];
const actions = new Set(["bookingSuccessfulV2", "eventTypeSelected", "linkReady", "linkFailed", "bookerReady"]);

export function prepare(url) {
    if (sdkUrl && sdkUrl !== url) throw new Error("Only one Cal.com embed SDK URL can be loaded per page.");
    sdkUrl = url;
    if (window.Cal) return;
    // Official command-queue protocol. ResourceLoader loads the script after this stub exists.
    const cal = function (...args) {
        if (args[0] === "init" && typeof args[1] === "string") {
            const name = args[1];
            const api = cal.ns[name] ??= Object.assign((...command) => api.q.push(command), { q: [] });
            api.q.push(args);
            cal.q.push(["initNamespace", name]);
        } else cal.q.push(args);
    };
    cal.q = [];
    cal.ns = {};
    cal.loaded = true;
    window.Cal = cal;
}

function getFrames(namespace) {
    return [...document.getElementsByName(`cal-embed=${namespace}`)].filter(el => el.tagName === "IFRAME" && el.isConnected);
}

export function create(id, configurationJson, dotnet) {
    const configuration = JSON.parse(configurationJson);
    if (!window.Cal?.instance) throw new Error("The Cal.com embed SDK has not finished loading.");
    const origin = new URL(configuration.origin).origin;
    const records = new Map();
    let sequence = 0;
    let disposed = false;
    const ensureActive = () => { if (disposed) throw new Error("Cal.com session is disposed."); };
    const read = json => {
        const options = JSON.parse(json);
        const link = options.calLink;
        if (typeof link !== "string" || !link.trim() || link.startsWith("/") || /[\\#\r\n\t]/.test(link))
            throw new Error("CalLink must be a relative booking path.");
        const path = decodeURIComponent(link.split("?")[0]);
        if (path.includes(":") || path.includes("\\") || path.startsWith("/") || path.split("/").some(part => part === "." || part === ".."))
            throw new Error("CalLink must stay within the configured Cal.com origin.");
        const url = new URL(link, origin + "/");
        if (url.origin !== origin) throw new Error("CalLink must stay within the configured Cal.com origin.");
        return options;
    };
    const uiConfig = (ui = {}) => ({
        theme: ui.theme ?? "auto", layout: ui.layout ?? "month_view",
        hideEventTypeDetails: ui.hideEventTypeDetails ?? false,
        disableAutoScroll: ui.disableAutoScroll ?? false,
        useSlotsViewOnSmallScreen: ui.useSlotsViewOnSmallScreen ?? false,
        cssVarsPerTheme: { light: ui.lightCssVariables ?? {}, dark: ui.darkCssVariables ?? {} }
    });
    const bookingConfig = options => {
        const { additionalFields, ...typed } = options.config ?? {};
        for (const key of Object.keys(additionalFields ?? {}))
            if (["iframeAttrs", "__proto__", "constructor", "prototype"].includes(key) || key.startsWith("cal.") || key.startsWith("flag."))
                throw new Error("AdditionalFields contains a reserved SDK parameter.");
        const ui = uiConfig(options.ui);
        return { ...additionalFields, ...typed, theme: ui.theme, layout: ui.layout };
    };
    const acquire = (key, kind, element = null) => {
        const index = idleNamespaces.findIndex(item => item.origin === origin && item.kind === kind);
        const namespace = index >= 0 ? idleNamespaces.splice(index, 1)[0].namespace : `soenneker-${id}-${++sequence}`;
        window.Cal("init", namespace, { origin });
        const record = { namespace, kind, element, key, pending: null };
        records.set(key, record);
        return record;
    };
    const command = (record, action, args) => window.Cal.ns[record.namespace](action, args);
    const removeModal = record => {
        record.pending?.abort();
        record.pending = null;
        command(record, "closeModal");
        for (const frame of getFrames(record.namespace)) frame.closest("cal-modal-box")?.remove();
    };
    const remove = key => {
        const record = records.get(key);
        if (!record) return;
        // Remove from event routing first, before invoking SDK cleanup.
        records.delete(key);
        if (record.kind === "inline") {
            record.element.replaceChildren();
            if (!record.hadContainerClass) record.element.classList.remove("cal-inline-container");
            containerOwners.delete(record.element);
        } else {
            removeModal(record);
            if (record.buttonId) document.getElementById(record.buttonId)?.remove();
        }
        idleNamespaces.push({ namespace: record.namespace, kind: record.kind, origin });
    };
    const onMessage = event => {
        const fullType = event.data?.fullType;
        if (disposed || typeof fullType !== "string" || event.origin !== origin) return;
        const [prefix, namespace, action, extra] = fullType.split(":");
        if (prefix !== "CAL" || extra !== undefined || !actions.has(action)) return;
        const record = [...records.values()].find(item => item.namespace === namespace);
        if (!record || !getFrames(namespace).some(frame => frame.contentWindow === event.source && new URL(frame.src).origin === event.origin)) return;
        dotnet.invokeMethodAsync("ReceiveEvent", action, JSON.stringify({
            namespace, elementId: record.kind === "inline" ? record.key.slice(7) : null, data: event.data.data ?? {}
        })).catch(error => { if (!disposed) console.error("Cal.com callback failed", error); });
    };
    window.addEventListener("message", onMessage);

    const waitForModal = record => new Promise((resolve, reject) => {
        const controller = new AbortController();
        record.pending = controller;
        const finish = error => {
            clearTimeout(timeout);
            observer.disconnect();
            record.pending = null;
            if (error) reject(error); else resolve();
        };
        const check = () => { if (getFrames(record.namespace).length) finish(); };
        const observer = new MutationObserver(check);
        const timeout = setTimeout(() => finish(new Error("Cal.com did not create its modal within 15 seconds.")), 15000);
        controller.signal.addEventListener("abort", () => finish(new Error("Cal.com modal was closed before initialization completed.")), { once: true });
        observer.observe(document.body, { childList: true, subtree: true });
        check();
    });
    return {
        createInline(elementId, json) {
            ensureActive();
            const options = read(json);
            const config = bookingConfig(options);
            const key = `inline:${elementId}`;
            const element = document.getElementById(elementId);
            if (!element) throw new Error(`Container '${elementId}' was not found.`);
            if (containerOwners.has(element) && containerOwners.get(element) !== id)
                throw new Error("The container belongs to another Cal.com service.");
            if (!records.has(key) && element.childNodes.length) throw new Error("Use an empty, dedicated inline container.");
            remove(key);
            const record = acquire(key, "inline", element);
            record.hadContainerClass = element.classList.contains("cal-inline-container");
            containerOwners.set(element, id);
            try {
                command(record, "inline", { calLink: options.calLink, elementOrSelector: element, config });
                command(record, "ui", uiConfig(options.ui));
                if (!getFrames(record.namespace).length) throw new Error("Cal.com could not create the inline embed.");
            } catch (error) { remove(key); throw error; }
        },
        destroyInline(elementId) { remove(`inline:${elementId}`); },
        async openPopup(json) {
            ensureActive();
            const options = read(json);
            const config = bookingConfig(options);
            remove("popup");
            const record = acquire("popup", "popup");
            try {
                command(record, "modal", { calLink: options.calLink, config });
                await waitForModal(record);
                command(record, "ui", uiConfig(options.ui));
            } catch (error) { if (records.get("popup") === record) remove("popup"); throw error; }
        },
        closePopup() { remove("popup"); },
        createFloatingButton(json) {
            ensureActive();
            const options = read(json);
            const config = bookingConfig(options);
            remove("floating");
            const record = acquire("floating", "floating");
            record.buttonId = `cal-button-${id}`;
            try {
                command(record, "floatingButton", { calLink: options.calLink, config,
                    buttonText: options.buttonText, hideButtonIcon: options.hideButtonIcon,
                    buttonPosition: options.buttonPosition, buttonColor: options.buttonColor,
                    buttonTextColor: options.buttonTextColor, attributes: { id: record.buttonId } });
                command(record, "ui", uiConfig(options.ui));
                if (!document.getElementById(record.buttonId)) throw new Error("Cal.com could not create its floating button.");
            } catch (error) { remove("floating"); throw error; }
        },
        destroyFloatingButton() { remove("floating"); },
        updateInlineUi(elementId, json) {
            ensureActive();
            const record = records.get(`inline:${elementId}`);
            if (!record) throw new Error("The inline widget has not been created.");
            command(record, "ui", uiConfig(JSON.parse(json)));
        },
        dispose() {
            if (disposed) return;
            disposed = true;
            window.removeEventListener("message", onMessage);
            for (const key of [...records.keys()]) remove(key);
            dotnet = null;
        }
    };
}
