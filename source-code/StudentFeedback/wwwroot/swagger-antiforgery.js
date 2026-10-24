
(function () {
    "use strict";

    const TOKEN_URL = "/api/antiforgery/token";
    const HEADER_NAME = "RequestVerificationToken";

    function isUnsafeMethod(method) {
        return ["POST", "PUT", "PATCH", "DELETE"]
            .includes((method || "").toUpperCase());
    }

    function shouldAttachToken(request) {
        if (!request || !isUnsafeMethod(request.method)) {
            return false;
        }

        // Chỉ gửi token cho API cùng website.
        const url = new URL(request.url, window.location.origin);

        return url.origin === window.location.origin &&
            url.pathname.startsWith("/api/");
    }

    async function fetchToken() {
        const response = await fetch(TOKEN_URL, {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store"
        });

        if (!response.ok) {
            throw new Error(
                "Không thể lấy antiforgery token. HTTP " +
                response.status
            );
        }

        const data = await response.json();

        if (!data.requestToken) {
            throw new Error(
                "API không trả về requestToken."
            );
        }

        return data.requestToken;
    }

    // Swagger UI requestInterceptor có thể trả về Promise.
    function configureSwagger() {
        if (!window.ui || !window.ui.getConfigs) {
            return false;
        }

        const config = window.ui.getConfigs();

        if (config.__studentFeedbackAntiforgeryConfigured) {
            return true;
        }

        const previousInterceptor = config.requestInterceptor;

        config.requestInterceptor = async function (request) {
            if (typeof previousInterceptor === "function") {
                request = await previousInterceptor(request);
            }

            if (!shouldAttachToken(request)) {
                return request;
            }

            const token = await fetchToken();

            request.headers = request.headers || {};
            request.headers[HEADER_NAME] = token;

            return request;
        };

        config.__studentFeedbackAntiforgeryConfigured = true;

        return true;
    }

    if (!configureSwagger()) {
        const interval = setInterval(function () {
            if (configureSwagger()) {
                clearInterval(interval);
            }
        }, 100);

        setTimeout(function () {
            clearInterval(interval);
        }, 10000);
    }
})();
