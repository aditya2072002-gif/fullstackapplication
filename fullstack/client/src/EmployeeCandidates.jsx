import { useCallback, useEffect, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, CircularProgress, Dialog, DialogActions,
    DialogContent, DialogTitle, Stack, Table, TableBody, TableCell, TableContainer,
    TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';

const initialValues = { username: '', password: '', firstName: '', lastName: '', email: '', phoneNumber: '', stack: '' };

export function EmployeeCandidates({ session, permissions = [] }) {
    const [candidates, setCandidates] = useState([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [passwordReceipt, setPasswordReceipt] = useState(null);
    const [editingCandidate, setEditingCandidate] = useState(null);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const canCreate = permissions.includes('Candidates.Create');
    const canUpdate = permissions.includes('Candidates.Update');
    const canDeactivate = permissions.includes('Candidates.Deactivate');
    const canDelete = permissions.includes('Candidates.Delete');

    const loadCandidates = useCallback(async (signal) => {
        try {
            setCandidates(await apiRequest('/api/employee/candidates', { token: session.accessToken, signal }));
            setError('');
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load your candidates.');
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadCandidates(controller.signal));
        return () => controller.abort();
    }, [loadCandidates]);

    async function createCandidate(values, { resetForm }) {
        setSaving(true);
        setError('');
        try {
            await apiRequest(editingCandidate ? `/api/employee/candidates/${editingCandidate.id}` : '/api/employee/candidates', {
                token: session.accessToken,
                method: editingCandidate ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(values),
            });
            if (!editingCandidate) setPasswordReceipt({ username: values.username, password: values.password });
            resetForm();
            setDialogOpen(false);
            setNotice(editingCandidate ? 'Candidate profile updated.' : 'Candidate created and assigned to you. Administrators have been notified.');
            setEditingCandidate(null);
            await loadCandidates();
        } catch (requestError) {
            setError(requestError.message || 'Could not create this candidate.');
        } finally {
            setSaving(false);
        }
    }

    async function copyInitialPassword() {
        try {
            await navigator.clipboard.writeText(passwordReceipt.password);
            setNotice('Initial password copied. Save it securely; it will not be shown again.');
        } catch (copyError) {
            setError(copyError.message || 'Could not copy the initial password.');
        }
    }

    async function deleteCandidate(candidate) {
        if (!window.confirm(`Permanently delete ${candidate.firstName} ${candidate.lastName}?`)) return;
        setError('');
        try {
            await apiRequest(`/api/employee/candidates/${candidate.id}`, { token: session.accessToken, method: 'DELETE' });
            setNotice('Inactive candidate profile deleted.');
            await loadCandidates();
        } catch (requestError) {
            setError(requestError.message || 'Could not delete the candidate.');
        }
    }

    async function toggleCandidateStatus(candidate) {
        setError('');
        try {
            await apiRequest(`/api/employee/candidates/${candidate.id}/status`, {
                token: session.accessToken,
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ isActive: !candidate.isActive }),
            });
            setNotice(candidate.isActive ? 'Candidate deactivated.' : 'Candidate activated.');
            await loadCandidates();
        } catch (requestError) {
            setError(requestError.message || 'Could not update candidate status.');
        }
    }

    return (
        <Stack spacing={2}>
            <Card elevation={0} sx={{ background: theme => `linear-gradient(120deg, ${theme.palette.info.main}12, ${theme.palette.secondary.main}16)`, border: '1px solid', borderColor: 'divider', transition: 'transform .2s, box-shadow .2s', '&:hover': { transform: 'translateY(-2px)', boxShadow: 4 } }}>
                <CardContent><Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <Box sx={{ flexGrow: 1 }}><Typography component="h2" variant="h5">My candidates</Typography><Typography color="text.secondary" sx={{ mt: 0.75 }}>You can view and mentor only candidates created by your employee account.</Typography></Box>
                    {canCreate && <Button onClick={() => { setEditingCandidate(null); setError(''); setDialogOpen(true); }} startIcon={<PortalIcon name="add" />} variant="contained">Add candidate</Button>}
                </Stack></CardContent>
            </Card>
            {error && <Alert severity="error">{error}</Alert>}
            {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
            <Card className="content-card" elevation={0}><CardContent>
                {loading ? <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading your candidates...</Typography></Box> : candidates.length === 0 ? (
                    <Stack alignItems="center" className="empty-state" spacing={1.5}><Box className="empty-state-icon"><PortalIcon name="directory" size={30} /></Box><Typography color="text.secondary">No candidates have been assigned to your mentor workspace yet.</Typography></Stack>
                ) : <TableContainer><Table size="small" aria-label="My candidates"><TableHead><TableRow><TableCell>Name</TableCell><TableCell>Username</TableCell><TableCell>Email</TableCell><TableCell>Phone</TableCell><TableCell>Stack</TableCell><TableCell>Status</TableCell><TableCell>Actions</TableCell></TableRow></TableHead>
                    <TableBody>{candidates.map(candidate => <TableRow hover key={candidate.id}>
                        <TableCell>{candidate.firstName} {candidate.lastName}</TableCell><TableCell>{candidate.username}</TableCell><TableCell>{candidate.email}</TableCell><TableCell>{candidate.phoneNumber || '—'}</TableCell><TableCell>{candidate.stack || '—'}</TableCell><TableCell>{candidate.isActive ? 'Active' : 'Inactive'}</TableCell>
                        <TableCell><Stack direction="row" spacing={1}>
                            {canUpdate && <Button onClick={() => { setEditingCandidate(candidate); setError(''); setDialogOpen(true); }} size="small">Edit</Button>}
                            {canDeactivate && <Button color={candidate.isActive ? 'warning' : 'success'} onClick={() => void toggleCandidateStatus(candidate)} size="small">{candidate.isActive ? 'Deactivate' : 'Activate'}</Button>}
                            {canDelete && !candidate.isActive && <Button color="error" onClick={() => void deleteCandidate(candidate)} size="small">Delete</Button>}
                        </Stack></TableCell>
                    </TableRow>)}</TableBody></Table></TableContainer>}
            </CardContent></Card>
            <Dialog fullWidth maxWidth="sm" onClose={() => !saving && setDialogOpen(false)} open={dialogOpen}>
                <Formik enableReinitialize initialValues={editingCandidate ? { ...initialValues, ...editingCandidate, password: '' } : initialValues} onSubmit={createCandidate} validate={values => {
                    const errors = {};
                    if (!editingCandidate && (values.username.trim().length < 3 || values.username.length > 50)) errors.username = 'Use 3 to 50 characters.';
                    if (!editingCandidate && (values.password.length < 12 || values.password.length > 128)) errors.password = 'Use 12 to 128 characters.';
                    ['firstName', 'lastName'].forEach(field => { if (!values[field].trim()) errors[field] = 'This field is required.'; });
                    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) errors.email = 'Enter a valid email address.';
                    if (!/^\d{10}$/.test(values.phoneNumber)) errors.phoneNumber = 'Enter a 10-digit phone number.';
                    return errors;
                }}>
                    {formik => <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                        <DialogTitle>{editingCandidate ? 'Update candidate profile' : 'Create candidate for my team'}</DialogTitle>
                        <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                            {(editingCandidate ? ['firstName', 'lastName', 'email', 'phoneNumber', 'stack'] : ['username', 'password', 'firstName', 'lastName', 'email', 'phoneNumber', 'stack']).map(field => {
                                const labels = { username: 'Username', password: 'Initial password', firstName: 'First name', lastName: 'Last name', email: 'Email', phoneNumber: 'Phone number', stack: 'Technology stack' };
                                const isPassword = field === 'password';
                                const errorText = formik.touched[field] && formik.errors[field];
                                return <TextField key={field} autoComplete={isPassword ? 'new-password' : field === 'username' ? 'off' : undefined} error={Boolean(errorText)} helperText={errorText || (isPassword ? 'At least 12 characters.' : field === 'phoneNumber' ? 'Enter 10 digits.' : undefined)} inputProps={field === 'phoneNumber' ? { maxLength: 10, inputMode: 'numeric' } : undefined} label={labels[field]} name={field} onBlur={formik.handleBlur} onChange={event => field === 'phoneNumber' ? formik.setFieldValue(field, event.target.value.replace(/\D/g, '').slice(0, 10)) : formik.handleChange(event)} required={['username', 'password', 'firstName', 'lastName', 'email', 'phoneNumber'].includes(field)} type={isPassword ? 'password' : field === 'email' ? 'email' : 'text'} value={formik.values[field]} />;
                            })}
                            <Typography color="text.secondary" variant="body2">Resume and experience fields are not collected. This candidate will be assigned to you as their mentor.</Typography>
                        </Stack></DialogContent>
                        <DialogActions><Button disabled={saving} onClick={() => setDialogOpen(false)}>Cancel</Button><Button disabled={saving} type="submit" variant="contained">{saving ? 'Creating...' : 'Create candidate'}</Button></DialogActions>
                    </Box>}
                </Formik>
            </Dialog>
            <Dialog onClose={() => { setPasswordReceipt(null); setNotice(''); }} open={Boolean(passwordReceipt)}>
                <DialogTitle>Candidate account created</DialogTitle>
                <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                    <Typography>Share this initial password securely with {passwordReceipt?.username}. It is shown only once and cannot be retrieved later.</Typography>
                    <TextField InputProps={{ readOnly: true }} label="Initial password" value={passwordReceipt?.password ?? ''} />
                </Stack></DialogContent>
                <DialogActions><Button onClick={() => void copyInitialPassword()}>Copy password</Button><Button onClick={() => setPasswordReceipt(null)} variant="contained">Done</Button></DialogActions>
            </Dialog>
        </Stack>
    );
}
