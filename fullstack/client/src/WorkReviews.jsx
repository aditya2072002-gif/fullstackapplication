import { useCallback, useEffect, useMemo, useState } from 'react';
import {
    Alert, Box, Button, Card, CardContent, Checkbox, Chip, CircularProgress, InputAdornment, MenuItem,
    Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';
import { StatusDonut } from './StatusDonut.jsx';

export function WorkReviews({ session, visible }) {
    const [entries, setEntries] = useState([]);
    const [candidates, setCandidates] = useState([]);
    const [selectedCandidateId, setSelectedCandidateId] = useState('');
    const [selectedIds, setSelectedIds] = useState([]);
    const [filters, setFilters] = useState({ name: '', stack: '', dateFrom: '', dateTo: '', status: '' });
    const [comment, setComment] = useState('');
    const [decisionComment, setDecisionComment] = useState('');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');

    const selectedCandidate = useMemo(() => candidates.find(candidate => String(candidate.id) === selectedCandidateId), [candidates, selectedCandidateId]);
    const taskCounts = useMemo(() => [
        { label: 'Approved', value: entries.filter(entry => entry.status === 'Approved').length },
        { label: 'Rejected', value: entries.filter(entry => entry.status === 'Rejected').length },
        { label: 'Pending', value: entries.filter(entry => entry.status === 'Pending').length },
    ], [entries]);
    const interviewCounts = useMemo(() => [
        { label: 'Pending review', value: candidates.filter(candidate => candidate.interviewDecision === 'PendingReview').length },
        { label: 'Interview qualified', value: candidates.filter(candidate => candidate.interviewDecision === 'InterviewQualified').length },
        { label: 'Not qualified', value: candidates.filter(candidate => candidate.interviewDecision === 'NotQualified').length },
    ], [candidates]);
    const interviewEligible = Boolean(selectedCandidate?.canSetInterviewDecision);

    const loadEntries = useCallback(async (signal) => {
        setLoading(true);
        try {
            const query = new URLSearchParams();
            Object.entries(filters).forEach(([key, value]) => { if (value !== '') query.set(key, value); });
            if (selectedCandidateId) query.set('candidateId', selectedCandidateId);
            const suffix = query.size ? `?${query.toString()}` : '';
            const result = await apiRequest(`/api/admin/work-tracking${suffix}`, { token: session.accessToken, signal });
            setEntries(result);
            setSelectedIds(current => current.filter(id => result.some(entry => entry.id === id && entry.status === 'Pending')));
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load work entries.');
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [filters, selectedCandidateId, session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        const candidateRoute = session.user.role === 'Employee' ? '/api/employee/candidates' : '/api/admin/candidates';
        apiRequest(candidateRoute, { token: session.accessToken, signal: controller.signal })
            .then(setCandidates)
            .catch(requestError => { if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load candidates.'); });
        return () => controller.abort();
    }, [session.accessToken, session.user.role]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadEntries(controller.signal));
        return () => controller.abort();
    }, [loadEntries, visible]);

    function updateFilter(field, value) {
        setFilters(current => ({ ...current, [field]: value }));
    }

    function toggleEntry(entry) {
        if (entry.status !== 'Pending') return;
        setSelectedIds(current => current.includes(entry.id)
            ? current.filter(id => id !== entry.id)
            : [...current, entry.id]);
    }

    async function saveReview(status) {
        if (selectedIds.length === 0) return;
        if (status === 'Rejected' && !comment.trim()) {
            setError('A comment is required when rejecting tasks.');
            return;
        }
        setSaving(true);
        setError('');
        setNotice('');
        try {
            await apiRequest('/api/admin/work-tracking/review', {
                token: session.accessToken,
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ candidateId: Number(selectedCandidateId), entryIds: selectedIds, status, comment }),
            });
            setSelectedIds([]);
            setComment('');
            setNotice(`${selectedIds.length} task${selectedIds.length === 1 ? '' : 's'} ${status.toLowerCase()}.`);
            await loadEntries();
            const route = session.user.role === 'Employee' ? '/api/employee/candidates' : '/api/admin/candidates';
            setCandidates(await apiRequest(route, { token: session.accessToken }));
        } catch (requestError) {
            setError(requestError.message || 'Could not save this review.');
        } finally {
            setSaving(false);
        }
    }

    async function saveInterviewDecision(decision) {
        if (!selectedCandidate || !interviewEligible) return;
        setSaving(true);
        setError('');
        setNotice('');
        try {
            await apiRequest(`/api/admin/candidates/${selectedCandidate.id}/interview-decision`, {
                token: session.accessToken,
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ decision, comment: decisionComment }),
            });
            setNotice('Interview qualification saved.');
            const route = session.user.role === 'Employee' ? '/api/employee/candidates' : '/api/admin/candidates';
            setCandidates(await apiRequest(route, { token: session.accessToken }));
            setDecisionComment('');
        } catch (requestError) {
            setError(requestError.message || 'Could not save the interview decision.');
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
                        <Typography color="text.secondary" sx={{ mt: 0.5 }}>Select a candidate to review pending tasks or record an interview qualification decision.</Typography>
                    </Box>
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                        <StatusDonut data={taskCounts} title="Task review stages" />
                        <StatusDonut data={interviewCounts} title="Interview qualification" />
                    </Stack>
                    {error && <Alert severity="error">{error}</Alert>}
                    {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
                    <Box className="review-filters">
                        <Box className="review-filter-field">
                            <Typography className="review-filter-label" component="label" htmlFor="review-candidate">Candidate</Typography>
                            <TextField id="review-candidate" onChange={event => { setSelectedCandidateId(event.target.value); setSelectedIds([]); }} select value={selectedCandidateId}>
                                <MenuItem value="">All candidates</MenuItem>
                                {candidates.map(candidate => <MenuItem key={candidate.id} value={String(candidate.id)}>{candidate.firstName} {candidate.lastName}</MenuItem>)}
                            </TextField>
                        </Box>
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
                            <Typography className="review-filter-label" component="label" htmlFor="review-filter-status">Review status</Typography>
                            <TextField id="review-filter-status" onChange={event => updateFilter('status', event.target.value)} select value={filters.status}>
                                <MenuItem value="">All statuses</MenuItem><MenuItem value="Pending">Pending</MenuItem><MenuItem value="Approved">Approved</MenuItem><MenuItem value="Rejected">Rejected</MenuItem>
                            </TextField>
                        </Box>
                        <Button onClick={() => { setFilters({ name: '', stack: '', dateFrom: '', dateTo: '', status: '' }); setSelectedCandidateId(''); setSelectedIds([]); }} variant="outlined">Clear filters</Button>
                    </Box>
                    {loading ? <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading reviews...</Typography></Box> : (
                        <TableContainer>
                            <Table className="work-review-table" size="small" aria-label="Candidate work entries to review">
                                <TableHead><TableRow><TableCell padding="checkbox">Select</TableCell><TableCell>Candidate</TableCell><TableCell>Mentor</TableCell><TableCell>Reviewed by</TableCell><TableCell>Stack</TableCell><TableCell>Date</TableCell><TableCell>Task</TableCell><TableCell>Hours</TableCell><TableCell>Review status / comment</TableCell><TableCell>Accomplishment</TableCell><TableCell>Learnings</TableCell><TableCell>Blockers / challenges</TableCell><TableCell>AI tools</TableCell></TableRow></TableHead>
                                <TableBody>
                                    {entries.map(entry => (
                                        <TableRow hover key={entry.id} onClick={() => toggleEntry(entry)} selected={selectedIds.includes(entry.id)} sx={{ cursor: entry.status === 'Pending' ? 'pointer' : 'default' }}>
                                            <TableCell padding="checkbox"><Checkbox aria-label={`Select ${entry.task} by ${entry.candidateFirstName}`} checked={selectedIds.includes(entry.id)} disabled={!selectedCandidate || entry.status !== 'Pending'} onClick={event => event.stopPropagation()} onChange={() => toggleEntry(entry)} /></TableCell>
                                            <TableCell>{entry.candidateFirstName} {entry.candidateLastName}</TableCell>
                                            <TableCell>{entry.mentorDisplayName || 'Admin-created'}</TableCell><TableCell>{entry.reviewedByDisplayName || 'Pending'}</TableCell>
                                            <TableCell>{entry.stack || '—'}</TableCell><TableCell>{entry.workDate}</TableCell><TableCell>{entry.task}</TableCell><TableCell>{entry.hoursSpent}</TableCell>
                                            <TableCell><Stack spacing={0.5}><Chip color={entry.status === 'Approved' ? 'success' : entry.status === 'Rejected' ? 'error' : 'warning'} label={entry.status} size="small" sx={{ width: 'fit-content' }} />{entry.reviewComment && <Typography variant="body2">{entry.reviewComment}</Typography>}</Stack></TableCell><TableCell>{entry.accomplishment}</TableCell><TableCell>{entry.learnings}</TableCell><TableCell>{entry.blockersAndChallenges}</TableCell><TableCell>{entry.aiToolsUsed || '—'}</TableCell>
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
                    {!selectedCandidate && <Alert severity="info">Select a candidate to review that candidate’s pending tasks and interview stage.</Alert>}
                    {selectedCandidate && <Stack spacing={2}>
                        <Box>
                            <Typography component="h3" variant="h6">{selectedCandidate.firstName} {selectedCandidate.lastName}</Typography>
                            <Stack direction="row" flexWrap="wrap" spacing={1} useFlexGap sx={{ mt: 1 }}>
                                <Chip color="info" label={`${entries.filter(entry => entry.candidateId === selectedCandidate.id && entry.status === 'Approved').length} approved`} />
                                <Chip color="error" label={`${entries.filter(entry => entry.candidateId === selectedCandidate.id && entry.status === 'Rejected').length} rejected`} />
                                <Chip color="warning" label={`${entries.filter(entry => entry.candidateId === selectedCandidate.id && entry.status === 'Pending').length} pending`} />
                                <Chip color={selectedCandidate.interviewDecision === 'InterviewQualified' ? 'success' : selectedCandidate.interviewDecision === 'NotQualified' ? 'error' : 'warning'} label={`Interview: ${selectedCandidate.interviewDecision === 'InterviewQualified' ? 'Interview qualified' : selectedCandidate.interviewDecision === 'NotQualified' ? 'Not qualified' : 'Pending review'}`} />
                            </Stack>
                        </Box>
                        {selectedCandidate.interviewDecision === 'PendingReview' && <Stack spacing={1.5}>
                            {interviewEligible ? <>
                                <TextField fullWidth label="Interview decision comment (optional)" inputProps={{ maxLength: 2000 }} minRows={2} multiline onChange={event => setDecisionComment(event.target.value)} value={decisionComment} />
                                <Stack direction="row" spacing={1}>
                                    <Button disabled={saving} onClick={() => saveInterviewDecision('InterviewQualified')} variant="contained" color="success">Qualify for interview</Button>
                                    <Button disabled={saving} onClick={() => saveInterviewDecision('NotQualified')} variant="contained" color="error">Mark not qualified</Button>
                                </Stack>
                            </> : <Alert severity="info">Interview qualification is available after the candidate’s 30-day submission window ends.</Alert>}
                        </Stack>}
                        {selectedIds.length > 0 && <Stack spacing={1.5}>
                            <Typography component="h4" variant="subtitle1">Review {selectedIds.length} selected pending task{selectedIds.length === 1 ? '' : 's'}</Typography>
                            <TextField error={!comment.trim() && error.includes('comment')} fullWidth label="Shared review comment (required for rejection)" inputProps={{ maxLength: 2000 }} minRows={2} multiline onChange={event => setComment(event.target.value)} value={comment} />
                            <Stack direction="row" justifyContent="flex-end" spacing={1}>
                                <Button disabled={saving} onClick={() => saveReview('Approved')} color="success" variant="contained">Approve selected</Button>
                                <Button disabled={saving} onClick={() => saveReview('Rejected')} color="error" variant="contained">Reject selected</Button>
                            </Stack>
                        </Stack>}
                    </Stack>}
                </Stack>
            </CardContent>
        </Card>
    );
}
