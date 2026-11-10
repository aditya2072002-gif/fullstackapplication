const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/+$/, '');

export class ApiError extends Error {
    constructor(message, status) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
    }
}

export async function apiRequest(url, { token, responseType = 'json', ...options } = {}) {
    const headers = new Headers(options.headers);
    if (token) {
        headers.set('Authorization', `Bearer ${token}`);
    }

    const requestUrl = apiBaseUrl ? new URL(url, `${apiBaseUrl}/`).toString() : url;
    headers.set('Cache-Control', 'no-store');
    const response = await fetch(requestUrl, { ...options, cache: 'no-store', headers });
    if (response.status === 401 && token) {
        window.dispatchEvent(new Event('portal:session-expired'));
    }
    if (!response.ok) {
        const body = await response.text();
        let message = `Request failed (HTTP ${response.status}).`;
        if (body) {
            try {
                const problem = JSON.parse(body);
                message = problem.title || problem.detail || message;
            } catch {
                message = body;
            }
        }
        throw new ApiError(message, response.status);
    }

    if (response.status === 204) {
        return null;
    }

    return responseType === 'blob' ? response.blob() : response.json();
}
