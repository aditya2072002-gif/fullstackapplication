import { useCallback, useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import {
    Alert, Badge, Box, CircularProgress, Divider, IconButton, List, ListItem, ListItemText,
    Popover, Stack, Typography,
} from '@mui/material';
import { dismissNotification, getNotifications, markNotificationsAsRead } from './api/notifications.js';
import { PortalIcon } from './PortalIcon.jsx';

export function NotificationsInbox({ session }) {
    const [anchorEl, setAnchorEl] = useState(null);
    const [notifications, setNotifications] = useState([]);
    const [loading, setLoading] = useState(true);
    const [dismissingId, setDismissingId] = useState(null);
    const [error, setError] = useState('');
    const isOpen = Boolean(anchorEl);
    const unreadCount = notifications.filter(notification => !notification.isRead).length;

    const loadNotifications = useCallback(async (signal) => {
        try {
            const result = await getNotifications(session.accessToken, signal);
            if (!signal?.aborted) {
                setNotifications(result);
                setError('');
            }
        } catch (requestError) {
            if (requestError.name !== 'AbortError' && !signal?.aborted) {
                setError(requestError.message || 'Could not load notifications.');
            }
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [session.accessToken]);

    useEffect(() => {
        const timeoutId = window.setTimeout(() => void loadNotifications(), 0);
        const intervalId = window.setInterval(() => void loadNotifications(), 15000);
        let active = true;
        const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim().replace(/\/+$/, '');
        const hubUrl = apiBaseUrl ? `${apiBaseUrl}/hubs/notifications` : '/hubs/notifications';
        const connection = new HubConnectionBuilder()
            .withUrl(hubUrl, { accessTokenFactory: () => session.accessToken })
            .withAutomaticReconnect()
            .build();
        connection.on('notification', notification => {
            setNotifications(current => current.some(item => item.id === notification.id)
                ? current
                : [notification, ...current]);
        });
        connection.onreconnected(() => { void loadNotifications(); });
        connection.start().catch(requestError => {
            if (active) setError(requestError.message || 'Real-time notifications are unavailable; inbox refresh will continue.');
        });
        return () => {
            active = false;
            window.clearTimeout(timeoutId);
            window.clearInterval(intervalId);
            void connection.stop();
        };
    }, [loadNotifications, session.accessToken]);

    async function openInbox(event) {
        setAnchorEl(event.currentTarget);
        setLoading(true);
        setError('');
        try {
            const result = await getNotifications(session.accessToken);
            setNotifications(result);
            if (result.some(notification => !notification.isRead)) {
                await markNotificationsAsRead(session.accessToken);
                setNotifications(result.map(notification => ({ ...notification, isRead: true })));
            }
        } catch (requestError) {
            setError(requestError.message || 'Could not load notifications.');
        } finally {
            setLoading(false);
        }
    }

    async function dismiss(id) {
        setDismissingId(id);
        setError('');
        try {
            await dismissNotification(id, session.accessToken);
            setNotifications(current => current.filter(notification => notification.id !== id));
        } catch (requestError) {
            setError(requestError.message || 'Could not dismiss this notification.');
        } finally {
            setDismissingId(null);
        }
    }

    return (
        <>
            <IconButton
                aria-label={`Open notifications, ${unreadCount} unread`}
                color="inherit"
                onClick={openInbox}
                size="large"
            >
                <Badge badgeContent={unreadCount} color="error" max={99}>
                    <PortalIcon name="notification" />
                </Badge>
            </IconButton>
            <Popover
                anchorEl={anchorEl}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                onClose={() => setAnchorEl(null)}
                open={isOpen}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
                slotProps={{ paper: { sx: { mt: 1, width: { xs: 'calc(100vw - 32px)', sm: 390 }, maxHeight: 520 } } }}
            >
                <Box sx={{ p: 2 }}>
                    <Typography component="h2" variant="h6">Notifications</Typography>
                    {error && <Alert severity="error" sx={{ mt: 1 }}>{error}</Alert>}
                </Box>
                <Divider />
                {loading ? (
                    <Stack alignItems="center" direction="row" justifyContent="center" spacing={1} sx={{ p: 3 }}>
                        <CircularProgress size={20} />
                        <Typography color="text.secondary">Loading notifications...</Typography>
                    </Stack>
                ) : notifications.length === 0 ? (
                    <Typography color="text.secondary" sx={{ p: 3, textAlign: 'center' }}>Your inbox is clear.</Typography>
                ) : (
                    <List disablePadding>
                        {notifications.map((notification, index) => (
                            <Box key={notification.id}>
                                {index > 0 && <Divider component="li" />}
                                <ListItem
                                    alignItems="flex-start"
                                    secondaryAction={(
                                        <IconButton
                                            aria-label="Delete notification"
                                            disabled={dismissingId === notification.id}
                                            edge="end"
                                            onClick={() => void dismiss(notification.id)}
                                            size="small"
                                        >
                                            <PortalIcon name="delete" size={18} />
                                        </IconButton>
                                    )}
                                    sx={{ pr: 7 }}
                                >
                                    <ListItemText
                                        primary={notification.message}
                                        primaryTypographyProps={{ fontWeight: notification.isRead ? 400 : 700 }}
                                        secondary={(
                                            <>
                                                <Typography
                                                    component="span"
                                                    display="block"
                                                    variant="body2"
                                                    sx={{ whiteSpace: 'pre-line', mt: 0.5 }}
                                                >
                                                    {notification.details}
                                                </Typography>
                                                <Typography component="span" color="text.secondary" display="block" variant="caption" sx={{ mt: 0.5 }}>
                                                    {new Date(notification.createdAtUtc).toLocaleString()}
                                                </Typography>
                                            </>
                                        )}
                                    />
                                </ListItem>
                            </Box>
                        ))}
                    </List>
                )}
            </Popover>
        </>
    );
}
