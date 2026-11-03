export class ApiError extends Error {
    constructor(message, status) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
    }
}

export async function apiRequest(url, { token, ...options } = {}) {
    const headers = new Headers(options.headers);
    if (token) {
        headers.set('Authorization', `Bearer ${token}`);
    }

    const response = await fetch(url, { ...options, headers });
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

    return response.json();
}
