import { useState } from 'react';
import { Dialog, DialogActions, DialogContent, DialogTitle, Button, Stack, Typography } from '@mui/material';
import { EmployeeCandidates } from './EmployeeCandidates.jsx';
import { PortalShell } from './PortalShell.jsx';
import { WorkReviews } from './WorkReviews.jsx';

export function EmployeePortal({ session, onLogout, darkMode, onDarkModeChange }) {
    const [activeMenu, setActiveMenu] = useState('candidates');
    const [profileOpen, setProfileOpen] = useState(false);

    return (
        <PortalShell
            activeMenu={activeMenu}
            darkMode={darkMode}
            onDarkModeChange={onDarkModeChange}
            onLogout={onLogout}
            onMenuChange={setActiveMenu}
            onProfile={() => setProfileOpen(true)}
            role="Employee"
            session={session}
            title="Mentor workspace"
        >
            {activeMenu === 'candidates'
                ? <EmployeeCandidates session={session} />
                : <WorkReviews session={session} visible />}
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
