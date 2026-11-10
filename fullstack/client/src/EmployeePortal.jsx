import { useEffect, useState } from 'react';
import { Alert, Box, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Button, Stack, Typography } from '@mui/material';
import { apiRequest } from './api.js';
import { EmployeeCandidates } from './EmployeeCandidates.jsx';
import { EmployeeManagement } from './EmployeeManagement.jsx';
import { PortalShell } from './PortalShell.jsx';
import { WorkReviews } from './WorkReviews.jsx';

export function EmployeePortal({ activeMenu, onMenuChange, session, onLogout, darkMode, onDarkModeChange }) {
    const [profileOpen, setProfileOpen] = useState(false);
    const [permissions, setPermissions] = useState(null);
    const [permissionError, setPermissionError] = useState('');

    useEffect(() => {
        const controller = new AbortController();
        apiRequest('/api/roles/permissions', { token: session.accessToken, signal: controller.signal })
            .then(result => setPermissions(result.permissions || []))
            .catch(error => { if (error.name !== 'AbortError') setPermissionError(error.message || 'Could not load employee permissions.'); });
        return () => controller.abort();
    }, [session.accessToken]);

    useEffect(() => {
        if (!permissions) return;
        const allowed = [
            ...(permissions.includes('Candidates.View') ? ['candidates'] : []),
            ...(permissions.includes('Reviews.Work') ? ['reviews'] : []),
            ...(permissions.some(permission => ['Team.View', 'Team.CreateEmployees', 'Team.ManageEmployees', 'Team.CreateRoles', 'Team.GrantPermissions', 'Team.ManageRolePermissions'].includes(permission)) ? ['employees'] : []),
        ];
        if (!allowed.includes(activeMenu) && allowed.length) onMenuChange(allowed[0]);
    }, [activeMenu, onMenuChange, permissions]);

    return (
        <PortalShell
            activeMenu={activeMenu}
            darkMode={darkMode}
            onDarkModeChange={onDarkModeChange}
            onLogout={onLogout}
            onMenuChange={onMenuChange}
            onProfile={() => setProfileOpen(true)}
            permissions={permissions || []}
            role="Employee"
            session={session}
            title="Mentor workspace"
        >
            {permissionError && <Alert severity="error">{permissionError}</Alert>}
            {!permissions && !permissionError && <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading access permissions...</Typography></Box>}
            {permissions && activeMenu === 'candidates' && permissions.includes('Candidates.View') && <EmployeeCandidates permissions={permissions} session={session} />}
            {permissions && activeMenu === 'reviews' && permissions.includes('Reviews.Work') && <WorkReviews permissions={permissions} session={session} visible />}
            {permissions && activeMenu === 'employees' && permissions.some(permission => ['Team.View', 'Team.CreateEmployees', 'Team.ManageEmployees', 'Team.CreateRoles', 'Team.GrantPermissions', 'Team.ManageRolePermissions'].includes(permission)) && <EmployeeManagement managedByEmployee session={session} />}
            {permissions && permissions.length === 0 && <Alert severity="info">Your employee account has no assigned permissions. Contact your administrator.</Alert>}
            <Dialog onClose={() => setProfileOpen(false)} open={profileOpen}>
                <DialogTitle>Employee profile</DialogTitle>
                <DialogContent><Stack spacing={1} sx={{ pt: 1 }}>
                    <Typography><strong>Name:</strong> {session.user.displayName}</Typography>
                    <Typography><strong>Username:</strong> {session.user.username || 'Not provided'}</Typography>
                    <Typography><strong>Role:</strong> Employee / Mentor</Typography>
                </Stack></DialogContent>
                <DialogActions><Button onClick={() => setProfileOpen(false)}>Close</Button></DialogActions>
            </Dialog>
        </PortalShell>
    );
}
