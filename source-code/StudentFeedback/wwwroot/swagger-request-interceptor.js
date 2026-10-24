
(function () {
    "use strict";

    const TOKEN_ENDPOINT = "/api/antiforgery/token";
    const TOKEN_HEADER = "RequestVerificationToken";

    async function attachAntiforgeryToken(request) {
        const method = (request.method || "GET").toUpperCase();

        if (!["POST", "PUT", "PATCH", "DELETE"].includes(method)) {
            return request;
        }

        const url = new URL(request.url, window.location.origin);

        if (
            url.origin !== window.location.origin ||
            !url.pathname.startsWith("/api/")
        ) {
            return request;
        }

        const response = await fetch(TOKEN_ENDPOINT, {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store"
        });

        if (!response.ok) {
            throw new Error(
                "Không lấy được antiforgery token: HTTP " +
                response.status
            );
        }

        const data = await response.json();

        if (!data.requestToken) {
            throw new Error("API không trả về requestToken.");
        }

        request.headers = request.headers || {};
        request.headers[TOKEN_HEADER] = data.requestToken;

        request.credentials = "same-origin";

        return request;
    }

    // Công khai interceptor để cấu hình lúc Swagger khởi tạo.
    window.studentFeedbackRequestInterceptor =
        attachAntiforgeryToken;
})();
