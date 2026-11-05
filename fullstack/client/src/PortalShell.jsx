import { useState } from 'react';
import {
    Avatar, Box, Button, Container, FormControlLabel, IconButton, Menu, MenuItem, Stack, Switch, Typography,
} from '@mui/material';
import { PortalIcon } from './PortalIcon.jsx';
import { NotificationsInbox } from './NotificationsInbox.jsx';

export function PortalShell({
    activeMenu,
    darkMode,
    onDarkModeChange,
    onLogout,
    onMenuChange,
    onProfile,
    role,
    session,
    title,
    children,
}) {
    const [profileAnchor, setProfileAnchor] = useState(null);
    const isAdmin = role === 'Admin';
    const sections = isAdmin
        ? [{ id: 'directory', label: 'Candidate list', icon: 'directory' }, { id: 'reviews', label: 'Work reviews', icon: 'review' }, { id: 'employees', label: 'Employees', icon: 'employee' }]
        : role === 'Employee'
            ? [{ id: 'candidates', label: 'My candidates', icon: 'directory' }, { id: 'reviews', label: 'Work reviews', icon: 'review' }]
            : [{ id: 'profile', label: 'Candidate profile', icon: 'candidate' }, { id: 'work', label: 'Work tracking', icon: 'work' }];
    const initials = (session.user.displayName || session.user.username || role).split(/\s+/).slice(0, 2).map(part => part[0]).join('').toUpperCase();

    return (
        <Box className={`app-shell portal-shell${darkMode ? ' theme-dark' : ''}`}>
            <Container maxWidth={false}>
                <Stack spacing={2}>
                    <Box className={`page-header ${isAdmin ? 'admin-page-header' : 'candidate-page-header'}`}>
                        <Box className="page-header-copy">
                            <Box className="portal-mark"><PortalIcon name={isAdmin ? 'admin' : 'candidate'} size={20} /></Box>
                            <Box>
                                <Typography className="eyebrow">{role} portal</Typography>
                                <Typography component="h1" variant="h4" className="page-title">{title}</Typography>
                            </Box>
                        </Box>
                        <Stack className="portal-header-actions" direction="row" alignItems="center" spacing={1}>
                            <NotificationsInbox session={session} />
                            <Typography className="portal-signed-in" color="text.secondary">{session.user.displayName}</Typography>
                            <IconButton aria-label="Open profile menu" onClick={event => setProfileAnchor(event.currentTarget)}>
                                <Avatar className="profile-avatar">{initials}</Avatar>
                            </IconButton>
                        </Stack>
                    </Box>
                    <Box className="portal-layout">
                        <Box component="nav" aria-label={`${role} sections`} className="portal-sidebar">
                            {sections.map(section => (
                                <Button
                                    aria-current={activeMenu === section.id ? 'page' : undefined}
                                    className={activeMenu === section.id ? 'portal-nav-button is-active' : 'portal-nav-button'}
                                    key={section.id}
                                    onClick={() => onMenuChange(section.id)}
                                    startIcon={<PortalIcon name={section.icon} />}
                                >{section.label}</Button>
                            ))}
                        </Box>
                        <Box component="main" className="portal-main">{children}</Box>
                    </Box>
                </Stack>
            </Container>
            <Menu anchorEl={profileAnchor} onClose={() => setProfileAnchor(null)} open={Boolean(profileAnchor)}>
                <MenuItem onClick={() => { setProfileAnchor(null); onProfile(); }}>{role === 'Admin' ? 'Admin profile' : role === 'Employee' ? 'Employee profile' : 'Candidate profile'}</MenuItem>
                <MenuItem disableRipple>
                    <FormControlLabel
                        control={<Switch checked={darkMode} onChange={event => onDarkModeChange(event.target.checked)} />}
                        label="Dark mode"
                        onClick={event => event.stopPropagation()}
                    />
                </MenuItem>
                <MenuItem onClick={() => { setProfileAnchor(null); onLogout(); }}>Log out</MenuItem>
            </Menu>
        </Box>
    );
}
