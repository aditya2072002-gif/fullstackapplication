const SESSION_STORAGE_KEY = 'portal-session';

export const DEFAULT_MENU_BY_ROLE = Object.freeze({
    Admin: 'overview',
    Employee: 'candidates',
    Candidate: 'profile',
});

const MENUS_BY_ROLE = Object.freeze({
    Admin: ['overview', 'directory', 'reviews', 'employees'],
    Employee: ['candidates', 'reviews'],
    Candidate: ['profile', 'work'],
});

export function isValidSession(session) {
    return Boolean(
        session?.accessToken
        && ['Admin', 'Employee', 'Candidate'].includes(session.user?.role)
        && Number.isFinite(Date.parse(session.expiresAtUtc))
        && Date.parse(session.expiresAtUtc) > Date.now(),
    );
}

export function loadSession() {
    try {
        const serialized = window.sessionStorage.getItem(SESSION_STORAGE_KEY);
        if (!serialized) return null;
        const session = JSON.parse(serialized);
        if (isValidSession(session)) return session;
    } catch (error) {
        console.error('Could not restore the portal session.', error);
    }

    clearSession();
    return null;
}

export function saveSession(session) {
    window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
}

export function clearSession() {
    window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
}

export function portalLocation(menu) {
    return `#/portal/${encodeURIComponent(menu)}`;
}

export function loginLocation() {
    return '#/login';
}

export function menuFromLocation(session, locationHash) {
    const match = /^#\/portal\/([^/?#]+)$/.exec(locationHash);
    const menu = match?.[1] ?? null;
    return MENUS_BY_ROLE[session?.user?.role]?.includes(menu) ? menu : null;
}

export function isLoginLocation(locationHash) {
    return locationHash === '#/login';
}
