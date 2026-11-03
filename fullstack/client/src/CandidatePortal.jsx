import { useCallback, useEffect, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, CircularProgress, Dialog, DialogActions,
    DialogContent, DialogTitle, Divider, Stack, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalShell } from './PortalShell.jsx';
import { WorkTracking } from './WorkTracking.jsx';

export function CandidatePortal({ session, onLogout, darkMode, onDarkModeChange }) {
    const [profile, setProfile] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const [passwordOpen, setPasswordOpen] = useState(false);
    const [resumeOpen, setResumeOpen] = useState(false);
    const [stackValue, setStackValue] = useState('');
    const [activeMenu, setActiveMenu] = useState('profile');

    const loadProfile = useCallback(async (signal) => {
        try {
            const result = await apiRequest('/api/candidates/me', { token: session.accessToken, signal });
            setProfile(result);
            setStackValue(result.stack || '');
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load your profile.');
        } finally {
            if (!signal?.aborted) setIsLoading(false);
        }
    }, [session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadProfile(controller.signal));
        return () => controller.abort();
    }, [loadProfile]);

    async function saveStack() {
        setIsSaving(true);
        setError('');
        setNotice('');
        try {
            const updated = await apiRequest('/api/candidates/me/stack', {
                token: session.accessToken,
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ stack: stackValue }),
            });
            setProfile(updated);
            setNotice('Technology stack updated.');
        } catch (requestError) {
            setError(requestError.message || 'Could not update your stack.');
        } finally {
            setIsSaving(false);
        }
    }

    async function changePassword(values) {
        setIsSaving(true);
        setError('');
        try {
            await apiRequest('/api/candidates/me/password', {
                token: session.accessToken,
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(values),
            });
            setPasswordOpen(false);
            setNotice('Password updated successfully.');
        } catch (requestError) {
            setError(requestError.message || 'Could not update your password.');
        } finally {
            setIsSaving(false);
        }
    }

    async function uploadResume(values, { resetForm }) {
        setIsSaving(true);
        setError('');
        const body = new FormData();
        body.append('Resume', values.resume);
        try {
            const updated = await apiRequest('/api/candidates/me/resume', {
                token: session.accessToken, method: 'PUT', body,
            });
            setProfile(updated);
            resetForm();
            setResumeOpen(false);
            setNotice('Resume uploaded successfully.');
        } catch (requestError) {
            setError(requestError.message || 'Could not upload the resume.');
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
            onMenuChange={setActiveMenu}
            onProfile={() => setActiveMenu('profile')}
            role="Candidate"
            session={session}
            title="My workspace"
        >
            <Stack spacing={2}>
                    {error && <Alert severity="error">{error}</Alert>}
                    {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
                    {isLoading ? (
                        <Box className="loading-state"><CircularProgress size={28} /><Typography>Loading your profile...</Typography></Box>
                    ) : activeMenu === 'work' ? <WorkTracking session={session} /> : profile ? (
                        <Card className="content-card" elevation={0}>
                            <CardContent>
                                <Stack spacing={2.5}>
                                    <Box>
                                        <Typography component="h2" variant="h5">Profile details</Typography>
                                        <Typography color="text.secondary" sx={{ mt: 0.5 }}>Your account details are managed by your administrator.</Typography>
                                    </Box>
                                    <Divider />
                                    <Box className="profile-grid">
                                        <ProfileField label="Name" value={`${profile.firstName} ${profile.lastName}`} />
                                        <ProfileField label="Username" value={profile.username} />
                                        <ProfileField label="Email" value={profile.email} />
                                        <ProfileField label="Phone number" value={profile.phoneNumber || 'Not provided'} />
                                        <ProfileField label="Experience" value={profile.experience || 'Not provided'} />
                                        <ProfileField label="Performance score" value={profile.performanceScore} />
                                        <ProfileField label="Resume" value={profile.resumeFileName || 'No resume uploaded'} />
                                    </Box>
                                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'flex-start' }}>
                                        <TextField fullWidth label="Technology stack" helperText="For example: React, .NET, SQL Server" inputProps={{ maxLength: 200 }} onChange={event => setStackValue(event.target.value)} value={stackValue} />
                                        <Button disabled={isSaving} onClick={saveStack} variant="outlined">{isSaving ? 'Saving...' : 'Save stack'}</Button>
                                    </Stack>
                                    <Divider />
                                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                                        <Button onClick={() => setPasswordOpen(true)} variant="contained">Change password</Button>
                                        <Button onClick={() => setResumeOpen(true)} variant="outlined">Upload / update resume</Button>
                                    </Stack>
                                </Stack>
                            </CardContent>
                        </Card>
                    ) : null}
            </Stack>
            <Dialog fullWidth maxWidth="sm" onClose={() => !isSaving && setPasswordOpen(false)} open={passwordOpen}>
                <Formik
                    initialValues={{ currentPassword: '', newPassword: '' }}
                    onSubmit={changePassword}
                    validate={values => {
                        const errors = {};
                        if (!values.currentPassword) errors.currentPassword = 'Current password is required.';
                        if (values.newPassword.length < 12 || values.newPassword.length > 128) errors.newPassword = 'New password must contain 12 to 128 characters.';
                        return errors;
                    }}
                >
                    {formik => <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                        <DialogTitle>Change password</DialogTitle>
                        <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                            <Typography color="text.secondary">Enter your current password and choose a new password with at least 12 characters.</Typography>
                            <TextField autoComplete="current-password" error={Boolean(formik.touched.currentPassword && formik.errors.currentPassword)} helperText={formik.touched.currentPassword && formik.errors.currentPassword} label="Current password" name="currentPassword" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="password" value={formik.values.currentPassword} />
                            <TextField autoComplete="new-password" error={Boolean(formik.touched.newPassword && formik.errors.newPassword)} helperText={formik.touched.newPassword && formik.errors.newPassword} inputProps={{ minLength: 12, maxLength: 128 }} label="New password" name="newPassword" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="password" value={formik.values.newPassword} />
                            {error && <Alert severity="error">{error}</Alert>}
                        </Stack></DialogContent>
                        <DialogActions><Button disabled={isSaving} onClick={() => setPasswordOpen(false)}>Cancel</Button><Button disabled={isSaving || formik.isSubmitting} type="submit" variant="contained">{isSaving ? 'Updating...' : 'Update password'}</Button></DialogActions>
                    </Box>}
                </Formik>
            </Dialog>
            <Dialog fullWidth maxWidth="sm" onClose={() => !isSaving && setResumeOpen(false)} open={resumeOpen}>
                <Formik initialValues={{ resume: null }} onSubmit={uploadResume} validate={values => values.resume ? {} : { resume: 'Choose a PDF or DOC resume to upload.' }}>
                    {formik => <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                        <DialogTitle>Upload or update resume</DialogTitle>
                        <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                            <Typography color="text.secondary">PDF or DOC files up to 10 MB are accepted.</Typography>
                            <TextField error={Boolean(formik.touched.resume && formik.errors.resume)} helperText={formik.touched.resume && formik.errors.resume} inputProps={{ accept: '.pdf,.doc,application/pdf,application/msword' }} label="Resume file" onBlur={() => formik.setFieldTouched('resume', true)} onChange={event => formik.setFieldValue('resume', event.target.files?.[0] || null)} required type="file" variant="outlined" />
                            {error && <Alert severity="error">{error}</Alert>}
                        </Stack></DialogContent>
                        <DialogActions><Button disabled={isSaving} onClick={() => setResumeOpen(false)}>Cancel</Button><Button disabled={isSaving || formik.isSubmitting} type="submit" variant="contained">{isSaving ? 'Uploading...' : 'Upload resume'}</Button></DialogActions>
                    </Box>}
                </Formik>
            </Dialog>
        </PortalShell>
    );
}

function ProfileField({ label, value }) {
    return <Box><Typography className="profile-label" variant="overline">{label}</Typography><Typography sx={{ overflowWrap: 'anywhere' }}>{value}</Typography></Box>;
}
