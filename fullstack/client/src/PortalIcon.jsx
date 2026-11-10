const iconPaths = {
    admin: <><path d="M12 3 4.5 6v5.2c0 4.3 3.1 8.3 7.5 9.8 4.4-1.5 7.5-5.5 7.5-9.8V6L12 3Z" /><path d="m9 12 2 2 4-4" /></>,
    candidate: <><circle cx="12" cy="8" r="3.5" /><path d="M5 20a7 7 0 0 1 14 0" /><path d="M18 5.5a3.5 3.5 0 0 1 0 5" /></>,
    employee: <><circle cx="9" cy="8" r="3" /><path d="M3 20a6 6 0 0 1 12 0M16 5.5a3 3 0 0 1 0 5M17 14a5 5 0 0 1 4 5" /></>,
    directory: <><rect x="3.5" y="4" width="17" height="16" rx="2" /><path d="M8 4v16M12 9h5M12 13h5M12 17h3" /></>,
    work: <><rect x="3" y="7" width="18" height="13" rx="2" /><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18M10 12v2h4v-2" /></>,
    review: <><path d="M4 5h16M4 10h10M4 15h8" /><circle cx="18" cy="17" r="3" /><path d="m16.8 17 1 1 1.6-2" /></>,
    chat: <><path d="M20 11.5a7.5 7.5 0 0 1-7.5 7.5H6l-3 2v-6a7.5 7.5 0 1 1 17-3.5Z" /><path d="M8 11h8M8 14.5h5" /></>,
    notification: <><path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" /><path d="M10 21h4" /></>,
    delete: <><path d="M4 7h16M10 11v6M14 11v6M5 7l1 14h12l1-14M9 7V4h6v3" /></>,
    search: <><circle cx="10.8" cy="10.8" r="6.3" /><path d="m16 16 4.2 4.2" /></>,
    add: <><circle cx="12" cy="12" r="9" /><path d="M12 8v8M8 12h8" /></>,
    empty: <><path d="M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Z" /><path d="m4.5 7.8 7.5 4.4 7.5-4.4M12 12.2V21M8 5.2l8 4.6" /></>,
};

export function PortalIcon({ name, className, size = 20 }) {
    return (
        <svg
            aria-hidden="true"
            className={className}
            fill="none"
            height={size}
            stroke="currentColor"
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="1.7"
            viewBox="0 0 24 24"
            width={size}
        >
            {iconPaths[name] || iconPaths.empty}
        </svg>
    );
}
