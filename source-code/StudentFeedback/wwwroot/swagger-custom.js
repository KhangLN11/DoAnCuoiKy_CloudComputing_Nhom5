
(function () {
    "use strict";

    const TOKEN_ENDPOINT = "/api/antiforgery/token";
    const TOKEN_HEADER = "RequestVerificationToken";

    const originalFetch = window.fetch.bind(window);

    function needsAntiforgery(input, init) {
        const request = input instanceof Request ? input : null;

        const url = new URL(
            request ? request.url : input,
            window.location.origin
        );

        const method = (
            init?.method ||
            request?.method ||
            "GET"
        ).toUpperCase();

        return (
            url.origin === window.location.origin &&
            url.pathname.startsWith("/api/") &&
            ["POST", "PUT", "PATCH", "DELETE"].includes(method)
        );
    }

    window.fetch = async function (input, init = {}) {
        if (!needsAntiforgery(input, init)) {
            return originalFetch(input, init);
        }

        const tokenResponse = await originalFetch(TOKEN_ENDPOINT, {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store"
        });

        if (!tokenResponse.ok) {
            throw new Error(
                "Không lấy được antiforgery token. HTTP " +
                tokenResponse.status
            );
        }

        const tokenData = await tokenResponse.json();

        if (!tokenData.requestToken) {
            throw new Error("Thiếu requestToken.");
        }

        const headers = new Headers(
            init.headers ||
            (input instanceof Request ? input.headers : undefined)
        );

        headers.set(TOKEN_HEADER, tokenData.requestToken);

        return originalFetch(input, {
            ...init,
            headers,
            credentials: "same-origin"
        });
    };
})();
