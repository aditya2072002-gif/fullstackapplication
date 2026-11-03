import { useCallback, useEffect, useMemo, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, CircularProgress, Dialog, DialogActions, DialogContent,
    DialogTitle, Radio, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
    TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';
import { WorkReportChart } from './WorkReportChart.jsx';

const emptyEntry = {
    workDate: new Date().toISOString().slice(0, 10),
    task: '',
    hoursSpent: '',
    accomplishment: '',
    learnings: '',
    blockersAndChallenges: '',
    aiToolsUsed: '',
};

export function WorkTracking({ session }) {
    const [entries, setEntries] = useState([]);
    const [selectedId, setSelectedId] = useState('');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const [deleteOpen, setDeleteOpen] = useState(false);
    const selectedEntry = entries.find(entry => String(entry.id) === selectedId);
    const reportData = useMemo(() => {
        const gradesByDate = entries.reduce((groups, entry) => {
            const grade = Number(entry.grade);
            if (entry.grade !== null && Number.isFinite(grade) && grade >= 0 && grade <= 100) {
                const scores = groups[entry.workDate] || { total: 0, count: 0 };
                scores.total += grade;
                scores.count += 1;
                groups[entry.workDate] = scores;
            }
            return groups;
        }, {});
        return Object.entries(gradesByDate).sort(([first], [second]) => first.localeCompare(second)).slice(-7)
            .map(([date, scores]) => ({ label: date.slice(5), value: Math.round(scores.total / scores.count) }));
    }, [entries]);

    const loadEntries = useCallback(async (signal) => {
        try {
            setEntries(await apiRequest('/api/candidates/me/work-tracking', { token: session.accessToken, signal }));
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load work entries.');
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadEntries(controller.signal));
        return () => controller.abort();
    }, [loadEntries]);

    function startNewEntry() {
        setSelectedId('');
        setError('');
        setNotice('');
    }

    function selectEntry(entry) {
        setSelectedId(String(entry.id));
        setError('');
        setNotice('');
    }

    async function deleteEntry() {
        if (!selectedEntry || selectedEntry.grade !== null) return;
        setSaving(true);
        setError('');
        setNotice('');
        try {
            await apiRequest(`/api/candidates/me/work-tracking/${selectedEntry.id}`, {
                token: session.accessToken,
                method: 'DELETE',
            });
            setDeleteOpen(false);
            startNewEntry();
            setNotice('Work entry deleted.');
            await loadEntries();
        } catch (requestError) {
            setError(requestError.message || 'Could not delete this work entry.');
        } finally {
            setSaving(false);
        }
    }

    async function saveEntry(values, { resetForm }) {
        setSaving(true);
        setError('');
        setNotice('');
        try {
            const editing = Boolean(selectedId);
            await apiRequest(editing ? `/api/candidates/me/work-tracking/${selectedId}` : '/api/candidates/me/work-tracking', {
                token: session.accessToken,
                method: editing ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ...values, hoursSpent: Number(values.hoursSpent) }),
            });
            const successMessage = editing ? 'Work entry updated and resubmitted for review.' : 'Work entry submitted for review.';
            resetForm({ values: { ...emptyEntry, workDate: new Date().toISOString().slice(0, 10) } });
            startNewEntry();
            setNotice(successMessage);
            await loadEntries();
        } catch (requestError) {
            setError(requestError.message || 'Could not save this work entry.');
        } finally {
            setSaving(false);
        }
    }

    return (
        <Card className="content-card" elevation={0}>
            <CardContent>
                <Stack spacing={2.5}>
                    <Box>
                        <Box className="section-title">
                            <Box className="section-icon"><PortalIcon name="work" /></Box>
                            <Typography component="h2" variant="h5">Work tracking</Typography>
                        </Box>
                        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                            Record completed work and submit it for your administrator to review. Editing a reviewed entry sends it back for review.
                        </Typography>
                    </Box>
                    <WorkReportChart data={reportData} description="Average reviewed grade by date for your most recent reviewed work." descriptionScale="0–100 points" maxValue={100} title="Grade report" valueLabel="points" />
                    {error && <Alert severity="error">{error}</Alert>}
                    {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
                    {loading ? (
                        <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading work entries...</Typography></Box>
                    ) : (
                        <TableContainer>
                            <Table className="work-entry-table" size="small" aria-label="Candidate work entries">
                                <TableHead><TableRow>
                                    <TableCell padding="checkbox" />
                                    <TableCell>Date</TableCell><TableCell>Task</TableCell><TableCell>Hours</TableCell>
                                    <TableCell>Accomplishment</TableCell><TableCell>Learnings</TableCell><TableCell>Blockers / challenges</TableCell><TableCell>AI tools</TableCell>
                                    <TableCell>Review</TableCell>
                                </TableRow></TableHead>
                                <TableBody>
                                    {entries.map(entry => (
                                        <TableRow hover key={entry.id} selected={String(entry.id) === selectedId} onClick={() => selectEntry(entry)} sx={{ cursor: 'pointer' }}>
                                            <TableCell padding="checkbox"><Radio aria-label={`Edit entry for ${entry.workDate}`} checked={String(entry.id) === selectedId} onChange={() => selectEntry(entry)} /></TableCell>
                                            <TableCell><TableValue value={entry.workDate} /></TableCell>
                                            <TableCell><TableValue value={entry.task} /></TableCell>
                                            <TableCell><TableValue value={String(entry.hoursSpent)} /></TableCell>
                                            <TableCell><TableValue value={entry.accomplishment} /></TableCell>
                                            <TableCell><TableValue value={entry.learnings} /></TableCell>
                                            <TableCell><TableValue value={entry.blockersAndChallenges} /></TableCell>
                                            <TableCell><TableValue value={entry.aiToolsUsed || '—'} /></TableCell>
                                            <TableCell><TableValue value={`${entry.grade === null ? 'Awaiting review' : `${entry.grade}/100`}${entry.adminFeedback ? ` — ${entry.adminFeedback}` : ''}`} /></TableCell>
                                        </TableRow>
                                    ))}
                                    {entries.length === 0 && <TableRow><TableCell colSpan={9}>
                                        <Stack alignItems="center" className="empty-state empty-state-inline" spacing={1}>
                                            <Box className="empty-state-icon"><PortalIcon name="empty" size={28} /></Box>
                                            <Typography color="text.secondary">No work entries yet. Add your first entry below.</Typography>
                                        </Stack>
                                    </TableCell></TableRow>}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    )}
                    <Formik
                        initialValues={selectedEntry ? {
                            workDate: selectedEntry.workDate,
                            task: selectedEntry.task,
                            hoursSpent: String(selectedEntry.hoursSpent),
                            accomplishment: selectedEntry.accomplishment,
                            learnings: selectedEntry.learnings,
                            blockersAndChallenges: selectedEntry.blockersAndChallenges,
                            aiToolsUsed: selectedEntry.aiToolsUsed || '',
                        } : { ...emptyEntry }}
                        key={selectedId || 'new-work-entry'}
                        onSubmit={saveEntry}
                        validate={values => {
                            const errors = {};
                            ['workDate', 'task', 'accomplishment', 'learnings', 'blockersAndChallenges'].forEach(field => {
                                if (!String(values[field] || '').trim()) errors[field] = 'This field is required.';
                            });
                            const hours = Number(values.hoursSpent);
                            if (!Number.isFinite(hours) || hours < 0.1 || hours > 24) errors.hoursSpent = 'Enter between 0.1 and 24 hours.';
                            return errors;
                        }}
                    >
                        {formik => <Box className="entry-form-panel" component="form" noValidate onSubmit={formik.handleSubmit}>
                            <Stack spacing={2}>
                                <Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={1}>
                                    <Box>
                                        <Typography component="h3" variant="h6">{selectedId ? 'Edit selected entry' : 'Add work entry'}</Typography>
                                        {selectedEntry?.grade !== null && selectedEntry && <Typography color="text.secondary" variant="body2">Reviewed entries cannot be deleted. Editing will resubmit this entry for review.</Typography>}
                                    </Box>
                                    <Stack direction="row" flexWrap="wrap" spacing={1} useFlexGap>
                                        {selectedId && <Button disabled={saving} onClick={startNewEntry} variant="outlined">Add new entry</Button>}
                                        {selectedEntry && <Button color="error" disabled={saving || selectedEntry.grade !== null} onClick={() => setDeleteOpen(true)} variant="outlined">Delete entry</Button>}
                                    </Stack>
                                </Stack>
                                <Box className="work-entry-grid-scroll">
                                    <Box className="work-entry-grid">
                                        <TextField error={Boolean(formik.touched.workDate && formik.errors.workDate)} helperText={formik.touched.workDate && formik.errors.workDate} InputLabelProps={{ shrink: true }} label="Date" name="workDate" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="date" value={formik.values.workDate} />
                                        <TextField error={Boolean(formik.touched.task && formik.errors.task)} helperText={formik.touched.task && formik.errors.task} inputProps={{ maxLength: 200 }} label="Task" name="task" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.task} />
                                        <TextField error={Boolean(formik.touched.hoursSpent && formik.errors.hoursSpent)} helperText={formik.touched.hoursSpent && formik.errors.hoursSpent} inputProps={{ min: 0.1, max: 24, step: 0.1 }} label="Hours spent" name="hoursSpent" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="number" value={formik.values.hoursSpent} />
                                        <TextField error={Boolean(formik.touched.accomplishment && formik.errors.accomplishment)} helperText={formik.touched.accomplishment && formik.errors.accomplishment} label="Accomplishment" minRows={2} multiline name="accomplishment" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.accomplishment} />
                                        <TextField error={Boolean(formik.touched.learnings && formik.errors.learnings)} helperText={formik.touched.learnings && formik.errors.learnings} label="Learnings" minRows={2} multiline name="learnings" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.learnings} />
                                        <TextField error={Boolean(formik.touched.blockersAndChallenges && formik.errors.blockersAndChallenges)} helperText={formik.touched.blockersAndChallenges && formik.errors.blockersAndChallenges} inputProps={{ maxLength: 2000 }} label="Blockers / challenges" minRows={2} multiline name="blockersAndChallenges" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.blockersAndChallenges} />
                                        <TextField label="AI tools used" name="aiToolsUsed" onBlur={formik.handleBlur} onChange={formik.handleChange} value={formik.values.aiToolsUsed} />
                                    </Box>
                                </Box>
                                <Stack direction="row" justifyContent="flex-end">
                                    <Button disabled={saving || formik.isSubmitting} startIcon={<PortalIcon name={selectedId ? 'work' : 'add'} />} type="submit" variant="contained">{saving ? 'Submitting...' : selectedId ? 'Update and resubmit' : 'Submit work entry'}</Button>
                                </Stack>
                            </Stack>
                        </Box>}
                    </Formik>
                </Stack>
            </CardContent>
            <Dialog onClose={() => !saving && setDeleteOpen(false)} open={deleteOpen}>
                <DialogTitle>Delete this work entry?</DialogTitle>
                <DialogContent>
                    <Typography>This will permanently delete “{selectedEntry?.task}”. This action cannot be undone.</Typography>
                    {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
                </DialogContent>
                <DialogActions>
                    <Button disabled={saving} onClick={() => setDeleteOpen(false)}>Cancel</Button>
                    <Button color="error" disabled={saving || selectedEntry?.grade !== null} onClick={deleteEntry} variant="contained">
                        {saving ? 'Deleting...' : 'Delete entry'}
                    </Button>
                </DialogActions>
            </Dialog>
        </Card>
    );
}

function TableValue({ value }) {
    return <Box className="table-cell-content" title={value}>{value}</Box>;
}
