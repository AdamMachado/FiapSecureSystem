(() => {
    const region = document.getElementById("app-toast-region");

    if (!region) {
        return;
    }

    const defaultTitles = {
        success: "Sucesso",
        warning: "Atenção",
        info: "Informação",
        error: "Erro"
    };

    const iconClasses = {
        success: "fa-solid fa-check",
        warning: "fa-solid fa-triangle-exclamation",
        info: "fa-solid fa-circle-info",
        error: "fa-solid fa-xmark"
    };

    const activeToasts = new Map();

    function normalizeType(type) {
        if (!type) {
            return "info";
        }

        const normalizedType = String(type).toLowerCase();
        return iconClasses[normalizedType] ? normalizedType : "info";
    }

    function createToastId() {
        return `toast-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    }

    function dismissToast(id) {
        const entry = activeToasts.get(id);
        if (!entry) {
            return;
        }

        window.clearTimeout(entry.timeoutId);
        entry.element.classList.add("is-closing");

        window.setTimeout(() => {
            entry.element.remove();
            activeToasts.delete(id);
        }, 220);
    }

    function scheduleDismiss(id, durationMs) {
        if (!Number.isFinite(durationMs) || durationMs <= 0) {
            return null;
        }

        return window.setTimeout(() => dismissToast(id), durationMs);
    }

    function buildToastElement(options, id) {
        const type = normalizeType(options.type);
        const title = options.title || defaultTitles[type];
        const message = String(options.message ?? "").trim();
        const toast = document.createElement("article");
        toast.className = "app-toast";
        toast.dataset.type = type;
        toast.dataset.toastId = id;
        toast.setAttribute("role", type === "error" || type === "warning" ? "alert" : "status");

        toast.innerHTML = `
            <span class="app-toast-icon" aria-hidden="true">
                <i class="${iconClasses[type]}"></i>
            </span>
            <div class="app-toast-content">
                <strong>${escapeHtml(title)}</strong>
                <p>${message}</p>
            </div>
            <button type="button" class="app-toast-close" aria-label="Fechar mensagem">
                <i class="fa-solid fa-xmark" aria-hidden="true"></i>
            </button>
        `;

        toast.querySelector(".app-toast-close")?.addEventListener("click", () => dismissToast(id));
        return toast;
    }

    function showToast(input) {
        console.log('showMessage()');
        const options = typeof input === "string"
            ? { message: input }
            : { ...input };

        console.log('showMessage options', options);

        const message = String(options.message ?? "").trim();
        if (!message) {
            console.log('no message to show, returning');
            return null;
        }

        const id = createToastId();
        const isSticky = Boolean(options.isSticky);
        const durationMs = isSticky ? 0 : Number(options.durationMs ?? 5000);
        const element = buildToastElement(options, id);
        console.log('created toast element', element);

        region.appendChild(element);

        window.requestAnimationFrame(() => {
            element.classList.add("is-visible");
        });

        activeToasts.set(id, {
            element,
            timeoutId: scheduleDismiss(id, durationMs)
        });

        return id;
    }

    function bootstrapToasts() {
        console.log('bootstrapToasts()');
        const bootstrapElements = document.querySelectorAll(".app-toast-bootstrap");

        console.log('bootstrapElements: ', bootstrapElements);

        for (const bootstrapElement of bootstrapElements) {
            try {
                const messages = JSON.parse(bootstrapElement.textContent ?? "[]");
                console.log('bootstrap messages: ', messages);

                if (Array.isArray(messages)) {
                    for (const message of messages) {
                        console.log('showing message', message);
                        showToast(message);
                    }
                }
                else {
                    console.log('messages is not an array', typeof messages);
                }
            }
            catch(e) {
                console.log('Error during show bootstrap toast: ', e);
            }

            bootstrapElement.remove();
        }
    }

    window.appToast = {
        show: showToast,
        dismiss: dismissToast,
        clear() {
            for (const toastId of [...activeToasts.keys()]) {
                dismissToast(toastId);
            }
        },
        success(message, options = {}) {
            return showToast({ ...options, type: "success", message });
        },
        warning(message, options = {}) {
            return showToast({ ...options, type: "warning", message });
        },
        info(message, options = {}) {
            return showToast({ ...options, type: "info", message });
        },
        error(message, options = {}) {
            return showToast({ ...options, type: "error", message });
        }
    };

    bootstrapToasts();
})();
