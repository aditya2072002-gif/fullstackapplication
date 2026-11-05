import { apiRequest } from '../api.js';

export function getNotifications(token, signal) {
    return apiRequest('/api/notifications', { token, signal });
}

export function markNotificationsAsRead(token) {
    return apiRequest('/api/notifications/read', { token, method: 'POST' });
}

export function dismissNotification(id, token) {
    return apiRequest(`/api/notifications/${id}`, { token, method: 'DELETE' });
}
