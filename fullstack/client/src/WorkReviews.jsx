import { useCallback, useEffect, useMemo, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, CircularProgress, InputAdornment, MenuItem, Radio,
    Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';
import { WorkReportChart } from './WorkReportChart.jsx';

export function WorkReviews({ session, visible }) {
    const [entries, setEntries] = useState([]);
    const [selectedId, setSelectedId] = useState('');
    const [filters, setFilters] = useState({ name: '', stack: '', dateFrom: '', dateTo: '', grade: '' });
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');

    const selectedEntry = useMemo(() => entries.find(entry => String(entry.id) === selectedId), [entries, selectedId]);
    const reportData = useMemo(() => {
        const reviewedByCandidate = entries.reduce((groups, entry) => {
            const gradeValue = Number(entry.grade);
            if (entry.grade !== null && Number.isFinite(gradeValue) && gradeValue >= 0 && gradeValue <= 100) {
                const candidate = `${entry.candidateFirstName} ${entry.candidateLastName}`;
                const group = groups[candidate] || { total: 0, count: 0 };
                group.total += gradeValue;
                group.count += 1;
                groups[candidate] = group;
            }
            return groups;
        }, {});
        return Object.entries(reviewedByCandidate).slice(0, 8).map(([label, scores]) => ({
            label,
            value: Math.round(scores.total / scores.count),
        }));
    }, [entries]);

    const loadEntries = useCallback(async (signal) => {
        setLoading(true);
        try {
            const query = new URLSearchParams();
            Object.entries(filters).forEach(([key, value]) => { if (value !== '') query.set(key, value); });
            const suffix = query.size ? `?${query.toString()}` : '';
            const result = await apiRequest(`/api/admin/work-tracking${suffix}`, { token: session.accessToken, signal });
            setEntries(result);
            setSelectedId(current => result.some(entry => String(entry.id) === current) ? current : '');
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load work entries.');
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [filters, session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadEntries(controller.signal));
        return () => controller.abort();
    }, [loadEntries, visible]);

    function updateFilter(field, value) {
        setFilters(current => ({ ...current, [field]: value }));
    }

    function selectEntry(entry) {
        setSelectedId(String(entry.id));
    }

    async function saveReview(values) {
        if (!selectedEntry) return;
        setSaving(true);
        setError('');
        setNotice('');
        try {
            await apiRequest(`/api/admin/work-tracking/${selectedEntry.id}/review`, {
                token: session.accessToken,
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ grade: Number(values.grade), adminFeedback: values.feedback }),
            });
            setNotice('Review saved. The candidate can now see the grade and feedback.');
            await loadEntries();
        } catch (requestError) {
            setError(requestError.message || 'Could not save this review.');
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
                            <Box className="section-icon"><PortalIcon name="review" /></Box>
                            <Typography component="h2" variant="h5">Work reviews</Typography>
                        </Box>
                        <Typography color="text.secondary" sx={{ mt: 0.5 }}>Filter submitted tasks, select an entry, and provide its one-time grade and feedback review.</Typography>
                    </Box>
                    <WorkReportChart data={reportData} description="Average grade per candidate for the work entries currently shown." maxValue={100} descriptionScale="0–100 points" title="Candidate review report" valueLabel="points" />
                    {error && <Alert severity="error">{error}</Alert>}
                    {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
                    <Box className="review-filters">
                        <Box className="review-filter-field">
                            <Typography className="review-filter-label" component="label" htmlFor="review-filter-name">Candidate name</Typography>
                            <TextField id="review-filter-name" InputProps={{ startAdornment: <InputAdornment position="start"><PortalIcon name="search" /></InputAdornment> }} onChange={event => updateFilter('name', event.target.value)} value={filters.name} />
                        </Box>
                        <Box className="review-filter-field">
                            <Typography className="review-filter-label" component="label" htmlFor="review-filter-stack">Stack</Typography>
                            <TextField id="review-filter-stack" onChange={event => updateFilter('stack', event.target.value)} value={filters.stack} />
                        </Box>
                        <Box className="review-filter-field">
                            <Typography className="review-filter-label" component="label" htmlFor="review-filter-date-from">Date from</Typography>
                            <TextField id="review-filter-date-from" onChange={event => updateFilter('dateFrom', event.target.value)} type="date" value={filters.dateFrom} />
                        </Box>
                        <Box className="review-filter-field">
                            <Typography className="review-filter-label" component="label" htmlFor="review-filter-date-to">Date to</Typography>
                            <TextField id="review-filter-date-to" onChange={event => updateFilter('dateTo', event.target.value)} type="date" value={filters.dateTo} />
                        </Box>
                        <Box className="review-filter-field">
                            <Typography className="review-filter-label" component="label" htmlFor="review-filter-grade">Exact grade</Typography>
                            <TextField id="review-filter-grade" inputProps={{ min: 0, max: 100 }} onChange={event => updateFilter('grade', event.target.value)} type="number" value={filters.grade} />
                        </Box>
                        <Button onClick={() => setFilters({ name: '', stack: '', dateFrom: '', dateTo: '', grade: '' })} variant="outlined">Clear filters</Button>
                    </Box>
                    {loading ? <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading reviews...</Typography></Box> : (
                        <TableContainer>
                            <Table className="work-review-table" size="small" aria-label="Candidate work entries to review">
                                <TableHead><TableRow><TableCell padding="checkbox"/><TableCell>Candidate</TableCell><TableCell>Mentor</TableCell><TableCell>Reviewed by</TableCell><TableCell>Stack</TableCell><TableCell>Date</TableCell><TableCell>Task</TableCell><TableCell>Hours</TableCell><TableCell>Grade</TableCell><TableCell>Accomplishment</TableCell><TableCell>Learnings</TableCell><TableCell>Blockers / challenges</TableCell><TableCell>AI tools</TableCell></TableRow></TableHead>
                                <TableBody>
                                    {entries.map(entry => (
                                        <TableRow hover key={entry.id} onClick={() => selectEntry(entry)} selected={String(entry.id) === selectedId} sx={{ cursor: 'pointer' }}>
                                            <TableCell padding="checkbox"><Radio aria-label={`Review ${entry.task} by ${entry.candidateFirstName}`} checked={String(entry.id) === selectedId} onChange={() => selectEntry(entry)} /></TableCell>
                                            <TableCell>{entry.candidateFirstName} {entry.candidateLastName}</TableCell>
                                            <TableCell>{entry.mentorDisplayName || 'Admin-created'}</TableCell><TableCell>{entry.reviewedByDisplayName || 'Pending'}</TableCell>
                                            <TableCell>{entry.stack || '—'}</TableCell><TableCell>{entry.workDate}</TableCell><TableCell>{entry.task}</TableCell><TableCell>{entry.hoursSpent}</TableCell>
                                            <TableCell>{entry.grade ?? 'Pending'}</TableCell><TableCell>{entry.accomplishment}</TableCell><TableCell>{entry.learnings}</TableCell><TableCell>{entry.blockersAndChallenges}</TableCell><TableCell>{entry.aiToolsUsed || '—'}</TableCell>
                                        </TableRow>
                                    ))}
                                    {entries.length === 0 && <TableRow><TableCell colSpan={13}>
                                        <Stack alignItems="center" className="empty-state empty-state-inline" spacing={1}>
                                            <Box className="empty-state-icon"><PortalIcon name="review" size={28} /></Box>
                                            <Typography color="text.secondary">No work entries match these filters.</Typography>
                                        </Stack>
                                    </TableCell></TableRow>}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    )}
                    {selectedEntry && <Formik
                        enableReinitialize
                        initialValues={{ grade: selectedEntry.grade === null ? '' : String(selectedEntry.grade), feedback: selectedEntry.adminFeedback || '' }}
                        key={selectedEntry.id}
                        onSubmit={saveReview}
                        validate={values => {
                            const gradeValue = Number(values.grade);
                            return values.grade === '' || !Number.isInteger(gradeValue) || gradeValue < 0 || gradeValue > 100
                                ? { grade: 'Choose a grade from 0 to 100.' }
                                : {};
                        }}
                    >
                        {formik => <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                            <Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                                <Typography component="h3" variant="h6" sx={{ flexGrow: 1 }}>Review: {selectedEntry.candidateFirstName} {selectedEntry.candidateLastName} — {selectedEntry.task}</Typography>
                                <TextField
                                    disabled={saving || selectedEntry.grade !== null}
                                    error={Boolean(formik.touched.grade && formik.errors.grade)}
                                    helperText={selectedEntry.grade !== null ? `Reviewed by ${selectedEntry.reviewedByDisplayName || 'an administrator or employee'}. This review is locked.` : formik.touched.grade && formik.errors.grade || 'Select a score from 0 to 100.'}
                                    InputLabelProps={{ shrink: true }}
                                    label="Grade (0–100)"
                                    name="grade"
                                    onBlur={formik.handleBlur}
                                    onChange={formik.handleChange}
                                    required
                                    select
                                    size="small"
                                    sx={{ minWidth: 190, '& .MuiSelect-select': { padding: '10px 36px 10px 14px' } }}
                                    value={formik.values.grade}
                                >
                                    {Array.from({ length: 101 }, (_, score) => <MenuItem key={score} value={String(score)}>{score}</MenuItem>)}
                                </TextField>
                            </Stack>
                            <TextField disabled={saving || selectedEntry.grade !== null} fullWidth label="Review feedback" minRows={2} multiline name="feedback" onChange={formik.handleChange} sx={{ mt: 2 }} value={formik.values.feedback} />
                            <Stack direction="row" justifyContent="flex-end" sx={{ mt: 2 }}><Button disabled={saving || formik.isSubmitting || selectedEntry.grade !== null} type="submit" variant="contained">{selectedEntry.grade !== null ? 'Review submitted' : saving ? 'Saving...' : 'Save review'}</Button></Stack>
                        </Box>}
                    </Formik>}
                </Stack>
            </CardContent>
        </Card>
    );
}
