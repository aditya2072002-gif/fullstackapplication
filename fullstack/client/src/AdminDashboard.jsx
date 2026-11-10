import { useCallback, useEffect, useMemo, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
    DialogTitle, FormControlLabel, InputAdornment, Radio, Stack, Switch, Table, TableBody,
    TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';
import { PortalShell } from './PortalShell.jsx';
import { WorkReviews } from './WorkReviews.jsx';
import { EmployeeManagement } from './EmployeeManagement.jsx';
import { AdminOverview } from './AdminOverview.jsx';
import { ProfileAvatar } from './ProfileAvatar.jsx';

const emptyForm = {
    username: '', password: '', firstName: '', lastName: '', email: '', phoneNumber: '',
    yearsOfExperience: '', stack: '', performanceScore: '0', isActive: true,
};

export function AdminDashboard({ activeMenu, onMenuChange, session, onLogout, darkMode, onDarkModeChange }) {
    const [candidates, setCandidates] = useState([]);
    const [selectedId, setSelectedId] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [downloadingResumeId, setDownloadingResumeId] = useState(null);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const [formOpen, setFormOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [passwordReceipt, setPasswordReceipt] = useState(null);
    const [editing, setEditing] = useState(false);
    const [candidateFilter, setCandidateFilter] = useState('');
    const [profileOpen, setProfileOpen] = useState(false);
    const [profilePhotoError, setProfilePhotoError] = useState('');

    const selectedCandidate = useMemo(
        () => candidates.find(candidate => String(candidate.id) === selectedId),
        [candidates, selectedId],
    );
    const filteredCandidates = useMemo(() => {
        const query = candidateFilter.trim().toLocaleLowerCase();
        if (!query) return candidates;
        return candidates.filter(candidate =>
            `${candidate.firstName} ${candidate.lastName} ${candidate.username} ${candidate.email} ${candidate.phoneNumber || ''} ${candidate.stack || ''} ${candidate.yearsOfExperience ?? ''}`
                .toLocaleLowerCase().includes(query));
    }, [candidateFilter, candidates]);

    const loadCandidates = useCallback(async (signal) => {
        try {
            const result = await apiRequest('/api/admin/candidates', { token: session.accessToken, signal });
            setCandidates(result);
            setSelectedId(current => result.some(candidate => String(candidate.id) === current) ? current : '');
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load candidate profiles.');
        } finally {
            if (!signal?.aborted) setIsLoading(false);
        }
    }, [session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadCandidates(controller.signal));
        return () => controller.abort();
    }, [loadCandidates]);

    function openCreateDialog() {
        setEditing(false);
        setFormOpen(true);
    }

    function openUpdateDialog() {
        if (!selectedCandidate) return;
        setEditing(true);
        setFormOpen(true);
    }

    async function saveCandidate(formValues) {
        setIsSaving(true);
        setError('');
        try {
            if (editing && !formValues.isActive) {
                const result = await apiRequest(`/api/admin/candidates/${selectedCandidate.id}/status`, {
                    token: session.accessToken,
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ isActive: false }),
                });
                setFormOpen(false);
                setNotice('Candidate deactivated. Profile details were unchanged.');
                await loadCandidates();
                setSelectedId(String(result.id));
                return;
            }

            const body = new FormData();
            const fields = ['firstName', 'lastName', 'email', 'yearsOfExperience', 'stack', 'performanceScore'];
            if (!editing || formValues.isActive) fields.push('phoneNumber');
            if (!editing) fields.push('username', 'password');
            if (editing) fields.push('isActive');
            fields.forEach(field => body.append(field, String(formValues[field] ?? '')));
            const result = await apiRequest(editing ? `/api/admin/candidates/${selectedCandidate.id}` : '/api/admin/candidates', {
                token: session.accessToken, method: editing ? 'PUT' : 'POST', body,
            });
            setFormOpen(false);
            if (!editing) setPasswordReceipt({ username: formValues.username, password: formValues.password });
            setNotice(editing ? 'Candidate profile updated.' : 'Candidate profile created. Save the initial password now; it will not be shown again.');
            await loadCandidates();
            setSelectedId(String(result.id));
        } catch (requestError) {
            setError(requestError.message || 'Could not save the candidate profile.');
        } finally {
            setIsSaving(false);
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

    async function downloadResume(candidate) {
        if (!candidate.resumeFileName || downloadingResumeId !== null) return;
        setDownloadingResumeId(candidate.id);
        setError('');
        try {
            const resume = await apiRequest(`/api/admin/candidates/${candidate.id}/resume`, {
                token: session.accessToken,
                responseType: 'blob',
            });
            const downloadUrl = URL.createObjectURL(resume);
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = candidate.resumeFileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.setTimeout(() => URL.revokeObjectURL(downloadUrl), 1000);
        } catch (requestError) {
            setError(requestError.message || 'Could not download the candidate resume.');
        } finally {
            setDownloadingResumeId(null);
        }
    }

    async function deleteCandidate() {
        if (!selectedCandidate || selectedCandidate.isActive) return;
        setIsSaving(true);
        setError('');
        try {
            await apiRequest(`/api/admin/candidates/${selectedCandidate.id}`, { token: session.accessToken, method: 'DELETE' });
            setDeleteOpen(false);
            setSelectedId('');
            setNotice('Inactive candidate record permanently deleted.');
            await loadCandidates();
        } catch (requestError) {
            setError(requestError.message || 'Could not delete the candidate.');
        } finally {
            setIsSaving(false);
        }
    }

    return (
        <PortalShell
            activeMenu={activeMenu}
            darkMode={darkMode}
            onDarkModeChange={onDarkModeChange}
            onLogout={onLogout}
            onMenuChange={onMenuChange}
            onProfile={() => setProfileOpen(true)}
            role="Admin"
            session={session}
            title="Admin workspace"
        >
            <>
                    {activeMenu === 'overview' && <AdminOverview onPhotoError={setProfilePhotoError} session={session} />}
                    {activeMenu === 'directory' && <Box className="content-card">
                        <Stack className="section-heading" direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                            <Box><Typography component="h2" variant="h5">Candidates</Typography><Typography color="text.secondary" sx={{ mt: 0.5 }}>Select a candidate to update or delete. Deactivate a profile before deleting it.</Typography></Box>
                            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                                <Button onClick={openCreateDialog} startIcon={<PortalIcon name="add" />} variant="contained">Add candidate</Button>
                                <Button disabled={!selectedCandidate} onClick={openUpdateDialog} variant="outlined">Update</Button>
                                <Button color="error" disabled={!selectedCandidate || selectedCandidate.isActive} onClick={() => setDeleteOpen(true)} variant="outlined">Delete</Button>
                            </Stack>
                        </Stack>
                        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
                        {profilePhotoError && <Alert onClose={() => setProfilePhotoError('')} severity="error" sx={{ mt: 2 }}>{profilePhotoError}</Alert>}
                        {notice && <Alert onClose={() => setNotice('')} severity="success" sx={{ mt: 2 }}>{notice}</Alert>}
                        {!isLoading && candidates.length > 0 && <TextField
                            fullWidth
                            InputProps={{ startAdornment: <InputAdornment position="start"><PortalIcon name="search" /></InputAdornment> }}
                            label="Search candidates"
                            onChange={event => setCandidateFilter(event.target.value)}
                            sx={{ mt: 2, maxWidth: 520 }}
                            value={candidateFilter}
                        />}
                        {isLoading ? <Box className="loading-state"><CircularProgress size={28} /><Typography>Loading candidates...</Typography></Box> : candidates.length === 0 ? (
                            <Stack alignItems="center" className="empty-state" spacing={1.5}>
                                <Box className="empty-state-icon"><PortalIcon name="directory" size={30} /></Box>
                                <Typography color="text.secondary">No candidates found. Select Add candidate to create the first profile.</Typography>
                            </Stack>
                        ) : <>
                            <Typography color="text.secondary" sx={{ mt: 2 }}>
                                Showing {filteredCandidates.length} of {candidates.length} candidates. Select a row to update or delete that profile.
                            </Typography>
                            <TableContainer className="directory-table-container">
                                <Table aria-label="Candidate profiles" className="directory-table" size="small">
                                    <TableHead><TableRow>
                                        <TableCell padding="checkbox" />
                                        <TableCell>Name</TableCell>
                                        <TableCell>Created by</TableCell>
                                        <TableCell>Username</TableCell>
                                        <TableCell>Email</TableCell>
                                        <TableCell>Phone</TableCell>
                                        <TableCell>Technology stack</TableCell>
                                        <TableCell>Years of experience</TableCell>
                                        <TableCell>Resume</TableCell>
                                        <TableCell>Performance score</TableCell>
                                        <TableCell>Status</TableCell>
                                        <TableCell>Inactive since</TableCell>
                                    </TableRow></TableHead>
                                    <TableBody>
                                        {filteredCandidates.map(candidate => {
                                            const candidateId = String(candidate.id);
                                            const isSelected = candidateId === selectedId;
                                            return <TableRow hover key={candidate.id} onClick={() => setSelectedId(candidateId)} selected={isSelected} sx={{ cursor: 'pointer' }}>
                                                <TableCell padding="checkbox"><Radio aria-label={`Select ${candidate.firstName} ${candidate.lastName}`} checked={isSelected} onChange={() => setSelectedId(candidateId)} value={candidateId} /></TableCell>
                                                <TableCell><Stack alignItems="center" direction="row" spacing={1.25}>
                                                    <ProfileAvatar id={candidate.id} name={`${candidate.firstName} ${candidate.lastName}`} onError={setProfilePhotoError} role="Candidate" size={36} token={session.accessToken} editable />
                                                    <DirectoryValue value={`${candidate.firstName} ${candidate.lastName}`} />
                                                </Stack></TableCell>
                                                <TableCell><DirectoryValue value={candidate.creatorDisplayName || 'Admin-created'} /></TableCell>
                                                <TableCell><DirectoryValue value={candidate.username} /></TableCell>
                                                <TableCell><DirectoryValue value={candidate.email} /></TableCell>
                                                <TableCell><DirectoryValue value={candidate.phoneNumber || '—'} /></TableCell>
                                                <TableCell><DirectoryValue value={candidate.stack || 'Not provided'} /></TableCell>
                                                <TableCell><DirectoryValue value={candidate.yearsOfExperience == null ? 'Not provided' : `${candidate.yearsOfExperience} years`} /></TableCell>
                                                <TableCell>
                                                    <Stack alignItems="center" direction="row" spacing={1}>
                                                        <DirectoryValue value={candidate.resumeFileName || 'No resume uploaded'} />
                                                        <Button
                                                            disabled={!candidate.resumeFileName || downloadingResumeId !== null}
                                                            onClick={event => {
                                                                event.stopPropagation();
                                                                downloadResume(candidate);
                                                            }}
                                                            size="small"
                                                        >
                                                            {downloadingResumeId === candidate.id ? 'Downloading...' : 'Download'}
                                                        </Button>
                                                    </Stack>
                                                </TableCell>
                                                <TableCell><DirectoryValue value={String(candidate.performanceScore)} /></TableCell>
                                                <TableCell><Chip color={candidate.isActive ? 'success' : 'default'} label={candidate.isActive ? 'Active' : 'Inactive'} size="small" /></TableCell>
                                                <TableCell><DirectoryValue value={candidate.inactiveAtUtc ? new Date(candidate.inactiveAtUtc).toLocaleDateString() : '—'} /></TableCell>
                                            </TableRow>;
                                        })}
                                        {filteredCandidates.length === 0 && <TableRow><TableCell colSpan={12} sx={{ color: 'text.secondary', py: 4, textAlign: 'center' }}>No candidates match that search.</TableCell></TableRow>}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        </>}
                    </Box>}
                    {activeMenu === 'reviews' && <WorkReviews session={session} visible />}
                    {activeMenu === 'employees' && <EmployeeManagement session={session} />}
            </>
            <Dialog onClose={() => setProfileOpen(false)} open={profileOpen}>
                <DialogTitle>Admin profile</DialogTitle>
                <DialogContent><Stack alignItems="center" spacing={1.5} sx={{ pt: 1 }}>
                    <ProfileAvatar id={session.user.id} name={session.user.displayName} onError={setProfilePhotoError} role="Admin" size={88} token={session.accessToken} username={session.user.username} editable />
                    <Typography color="text.secondary" variant="caption">Your profile photo is saved with your account and available across devices.</Typography>
                    {profilePhotoError && <Alert onClose={() => setProfilePhotoError('')} severity="error">{profilePhotoError}</Alert>}
                    <Typography><strong>Name:</strong> {session.user.displayName}</Typography>
                    <Typography><strong>Username:</strong> {session.user.username || 'Not provided'}</Typography>
                    <Typography><strong>Role:</strong> Administrator</Typography>
                </Stack></DialogContent>
                <DialogActions><Button onClick={() => setProfileOpen(false)}>Close</Button></DialogActions>
            </Dialog>
            <Dialog fullWidth maxWidth="sm" onClose={() => !isSaving && setFormOpen(false)} open={formOpen}>
                <Formik
                    enableReinitialize
                    initialValues={editing && selectedCandidate
                        ? { ...emptyForm, ...selectedCandidate, password: '', yearsOfExperience: selectedCandidate.yearsOfExperience == null ? '' : String(selectedCandidate.yearsOfExperience), performanceScore: String(selectedCandidate.performanceScore) }
                        : { ...emptyForm }}
                    onSubmit={saveCandidate}
                    validate={values => {
                        if (editing && !values.isActive) return {};
                        const errors = {};
                        ['firstName', 'lastName', 'email'].forEach(field => {
                            if (!values[field]?.trim()) errors[field] = 'This field is required.';
                        });
                        const yearsOfExperience = Number(values.yearsOfExperience);
                        if (!Number.isInteger(yearsOfExperience) || yearsOfExperience < 1 || yearsOfExperience > 99) {
                            errors.yearsOfExperience = 'Enter a whole number from 1 to 99.';
                        }
                        if ((!editing || values.isActive) && !/^\d{10}$/.test(values.phoneNumber || '')) errors.phoneNumber = 'Enter a 10-digit phone number.';
                        if (!editing && (values.username.trim().length < 3 || values.username.length > 50)) errors.username = 'Enter a username with 3 to 50 characters.';
                        if (!editing && (values.password.length < 12 || values.password.length > 128)) errors.password = 'Password must contain 12 to 128 characters.';
                        if (values.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) errors.email = 'Enter a valid email address.';
                        const score = Number(values.performanceScore);
                        if (!Number.isFinite(score) || score < 0 || score > 100) errors.performanceScore = 'Enter a score from 0 to 100.';
                        return errors;
                    }}
                >
                    {formik => {
                        const fieldsDisabled = isSaving || (editing && !formik.values.isActive);
                        return <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                            <DialogTitle>{editing ? 'Update candidate profile' : 'Add candidate'}</DialogTitle>
                            <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                                {!editing && <TextField autoComplete="off" disabled={isSaving} error={Boolean(formik.touched.username && formik.errors.username)} helperText={formik.touched.username && formik.errors.username} inputProps={{ minLength: 3, maxLength: 50 }} label="Username" name="username" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.username} />}
                                {!editing && <TextField autoComplete="new-password" disabled={isSaving} error={Boolean(formik.touched.password && formik.errors.password)} helperText={formik.touched.password && formik.errors.password || 'At least 12 characters.'} inputProps={{ minLength: 12, maxLength: 128 }} label="Initial password" name="password" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="password" value={formik.values.password} />}
                                <TextField disabled={fieldsDisabled} error={Boolean(formik.touched.firstName && formik.errors.firstName)} helperText={formik.touched.firstName && formik.errors.firstName} label="First name" name="firstName" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.firstName} inputProps={{ maxLength: 50 }} />
                                <TextField disabled={fieldsDisabled} error={Boolean(formik.touched.lastName && formik.errors.lastName)} helperText={formik.touched.lastName && formik.errors.lastName} label="Last name" name="lastName" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.lastName} inputProps={{ maxLength: 50 }} />
                                <TextField disabled={fieldsDisabled} error={Boolean(formik.touched.email && formik.errors.email)} helperText={formik.touched.email && formik.errors.email} label="Email" name="email" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="email" value={formik.values.email} inputProps={{ maxLength: 254 }} />
                                <TextField disabled={fieldsDisabled} error={Boolean(formik.touched.phoneNumber && formik.errors.phoneNumber)} helperText={formik.touched.phoneNumber && formik.errors.phoneNumber || 'Enter 10 digits.'} inputProps={{ inputMode: 'numeric', maxLength: 10, pattern: '[0-9]{10}' }} label="Phone number" name="phoneNumber" onBlur={formik.handleBlur} onChange={event => formik.setFieldValue('phoneNumber', event.target.value.replace(/\D/g, '').slice(0, 10))} required={!editing || Boolean(formik.values.isActive)} type="tel" value={formik.values.phoneNumber || ''} />
                                <TextField disabled={fieldsDisabled} error={Boolean(formik.touched.yearsOfExperience && formik.errors.yearsOfExperience)} helperText={formik.touched.yearsOfExperience && formik.errors.yearsOfExperience || 'Enter a whole number from 1 to 99.'} inputProps={{ min: 1, max: 99, step: 1 }} label="Years of experience" name="yearsOfExperience" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="number" value={formik.values.yearsOfExperience} />
                                <TextField disabled={fieldsDisabled} helperText="For example: React, .NET, SQL Server" label="Technology stack" name="stack" onBlur={formik.handleBlur} onChange={formik.handleChange} value={formik.values.stack} inputProps={{ maxLength: 200 }} />
                                <TextField disabled={fieldsDisabled} error={Boolean(formik.touched.performanceScore && formik.errors.performanceScore)} helperText={formik.touched.performanceScore && formik.errors.performanceScore} label="Performance score" name="performanceScore" inputProps={{ min: 0, max: 100 }} onBlur={formik.handleBlur} onChange={formik.handleChange} required type="number" value={formik.values.performanceScore} />
                                {editing && <FormControlLabel control={<Switch checked={Boolean(formik.values.isActive)} disabled={isSaving} onChange={event => formik.setFieldValue('isActive', event.target.checked)} />} label={formik.values.isActive ? 'Active' : 'Inactive'} />}
                            </Stack></DialogContent>
                            <DialogActions><Button disabled={isSaving} onClick={() => setFormOpen(false)}>Cancel</Button><Button disabled={isSaving} type="submit" variant="contained">{isSaving ? 'Saving...' : 'Save'}</Button></DialogActions>
                        </Box>;
                    }}
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
            <Dialog onClose={() => !isSaving && setDeleteOpen(false)} open={deleteOpen}>
                <DialogTitle>Permanently delete candidate?</DialogTitle>
                <DialogContent><Typography>This will permanently remove {selectedCandidate?.firstName} {selectedCandidate?.lastName} and their profile. This cannot be undone.</Typography>{error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}</DialogContent>
                <DialogActions><Button disabled={isSaving} onClick={() => setDeleteOpen(false)}>Cancel</Button><Button color="error" disabled={isSaving || selectedCandidate?.isActive} onClick={deleteCandidate} variant="contained">{isSaving ? 'Deleting...' : 'Delete permanently'}</Button></DialogActions>
            </Dialog>
        </PortalShell>
    );
}

function DirectoryValue({ value }) {
    return <Box className="table-cell-content" title={value}>{value}</Box>;
}
