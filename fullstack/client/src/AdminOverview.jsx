import { useEffect, useMemo, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, Chip, CircularProgress, FormControl, InputLabel,
    MenuItem, Select, Stack, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';
import { ProfileAvatar } from './ProfileAvatar.jsx';

const colors = {
    candidate: '#6c63ff',
    employee: '#19a7a1',
    active: '#26a879',
    inactive: '#f07167',
    reviewed: '#4278e8',
    pending: '#f3aa45',
    submitted: '#8068e8',
};

function localDateValue(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function displayDate(value) {
    return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
    });
}

const today = new Date();
const monthAgo = new Date(today);
monthAgo.setDate(monthAgo.getDate() - 29);
const initialFilters = { dateFrom: localDateValue(monthAgo), dateTo: localDateValue(today), candidateId: '' };

function candidateName(candidate) {
    return `${candidate.firstName || ''} ${candidate.lastName || ''}`.trim() || candidate.username || 'Candidate';
}

function withinRange(date, from, to) {
    return Boolean(date && date >= from && date <= to);
}

export function AdminOverview({ session, onPhotoError }) {
    const [candidates, setCandidates] = useState([]);
    const [employees, setEmployees] = useState([]);
    const [entries, setEntries] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [photoError, setPhotoError] = useState('');
    const [filters, setFilters] = useState(initialFilters);

    function reportPhotoError(message) {
        setPhotoError(message);
        onPhotoError?.(message);
    }

    useEffect(() => {
        const controller = new AbortController();
        async function loadDashboard() {
            setLoading(true);
            setError('');
            try {
                const [candidateData, employeeData, entryData] = await Promise.all([
                    apiRequest('/api/admin/candidates', { token: session.accessToken, signal: controller.signal }),
                    apiRequest('/api/admin/employees', { token: session.accessToken, signal: controller.signal }),
                    apiRequest('/api/admin/work-tracking', { token: session.accessToken, signal: controller.signal }),
                ]);
                setCandidates(candidateData);
                setEmployees(employeeData);
                setEntries(entryData);
            } catch (requestError) {
                if (requestError.name !== 'AbortError') {
                    setError(requestError.message || 'Could not load dashboard data.');
                }
            } finally {
                if (!controller.signal.aborted) setLoading(false);
            }
        }

        void loadDashboard();
        return () => controller.abort();
    }, [session.accessToken]);

    const candidateById = useMemo(
        () => new Map(candidates.map(candidate => [String(candidate.id), candidate])),
        [candidates],
    );
    const selectedCandidate = filters.candidateId ? candidateById.get(filters.candidateId) : null;
    const candidateEntries = useMemo(() => entries.filter(entry =>
        (!filters.candidateId || String(entry.candidateId) === filters.candidateId)), [entries, filters.candidateId]);
    const rangeEntries = useMemo(() => candidateEntries.filter(entry =>
        withinRange(entry.workDate, filters.dateFrom, filters.dateTo)), [candidateEntries, filters.dateFrom, filters.dateTo]);
    const approvedCount = rangeEntries.filter(entry => entry.status === 'Approved').length;
    const rejectedCount = rangeEntries.filter(entry => entry.status === 'Rejected').length;
    const pendingCount = rangeEntries.filter(entry => entry.status === 'Pending').length;
    const reviewedCount = approvedCount + rejectedCount;
    const pendingInterviewCount = candidates.filter(candidate => candidate.interviewDecision === 'PendingReview').length;
    const qualifiedInterviewCount = candidates.filter(candidate => candidate.interviewDecision === 'InterviewQualified').length;
    const notQualifiedInterviewCount = candidates.filter(candidate => candidate.interviewDecision === 'NotQualified').length;
    const activeCandidates = candidates.filter(candidate => candidate.isActive).length;
    const activeEmployees = employees.filter(employee => employee.isActive).length;
    const visibleCandidates = filters.candidateId
        ? candidates.filter(candidate => String(candidate.id) === filters.candidateId)
        : candidates;

    function applyFilters(values) {
        setFilters(values);
    }

    return (
        <Stack className="admin-overview" spacing={2.5}>
            <Card className="overview-welcome" elevation={0}>
                <CardContent>
                    <Stack alignItems={{ xs: 'flex-start', md: 'center' }} direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
                        <Box>
                            <Typography className="overview-kicker">Workspace overview</Typography>
                            <Typography component="h2" variant="h4">Good to see you, {session.user.displayName?.split(/\s+/)[0] || 'Admin'}</Typography>
                            <Typography sx={{ mt: 0.75 }} variant="body2">A live view of your people, candidate activity, and reviews.</Typography>
                        </Box>
                        <Box className="overview-date-badge">
                            <Typography variant="caption">REPORTING PERIOD</Typography>
                            <Typography fontWeight={700}>{displayDate(filters.dateFrom)} — {displayDate(filters.dateTo)}</Typography>
                        </Box>
                    </Stack>
                </CardContent>
            </Card>

            <Card className="overview-filter-card" elevation={0}>
                <CardContent>
                    <Formik
                        initialValues={initialFilters}
                        onSubmit={applyFilters}
                        validate={values => {
                            const validationErrors = {};
                            if (!values.dateFrom || !values.dateTo) validationErrors.dateTo = 'Choose a start and end date.';
                            else if (values.dateFrom > values.dateTo) validationErrors.dateTo = 'End date must be on or after the start date.';
                            return validationErrors;
                        }}
                    >
                        {formik => <Box component="form" className="overview-filters" onSubmit={formik.handleSubmit}>
                            <FormControl fullWidth size="small">
                                <InputLabel id="overview-candidate-label">Candidate</InputLabel>
                                <Select label="Candidate" labelId="overview-candidate-label" name="candidateId" onChange={formik.handleChange} value={formik.values.candidateId}>
                                    <MenuItem value="">All candidates</MenuItem>
                                    {candidates.map(candidate => <MenuItem key={candidate.id} value={String(candidate.id)}>{candidateName(candidate)}</MenuItem>)}
                                </Select>
                            </FormControl>
                            <TextField
                                fullWidth
                                error={Boolean(formik.touched.dateFrom && formik.errors.dateFrom)}
                                helperText={formik.touched.dateFrom && formik.errors.dateFrom}
                                label="From"
                                name="dateFrom"
                                onBlur={formik.handleBlur}
                                onChange={formik.handleChange}
                                size="small"
                                type="date"
                                value={formik.values.dateFrom}
                                slotProps={{ inputLabel: { shrink: true } }}
                            />
                            <TextField
                                fullWidth
                                error={Boolean(formik.touched.dateTo && formik.errors.dateTo)}
                                helperText={formik.touched.dateTo && formik.errors.dateTo}
                                label="To"
                                name="dateTo"
                                onBlur={formik.handleBlur}
                                onChange={formik.handleChange}
                                size="small"
                                type="date"
                                value={formik.values.dateTo}
                                slotProps={{ inputLabel: { shrink: true } }}
                            />
                            <Button disabled={loading} type="submit" variant="contained">Apply filters</Button>
                        </Box>}
                    </Formik>
                </CardContent>
            </Card>

            {error && <Alert severity="error">{error}</Alert>}
            {photoError && <Alert onClose={() => setPhotoError('')} severity="error">{photoError}</Alert>}

            {loading ? <Box className="loading-state"><CircularProgress size={28} /><Typography>Loading workforce insights...</Typography></Box> : <>
                <Box className="overview-kpi-grid">
                    <MetricCard color={colors.candidate} icon="candidate" label="Total candidates" value={candidates.length} detail={`${activeCandidates} active · ${candidates.length - activeCandidates} inactive`} />
                    <MetricCard color={colors.active} icon="candidate" label="Active candidates" value={activeCandidates} detail={`${percent(activeCandidates, candidates.length)}% of candidate roster`} />
                    <MetricCard color={colors.employee} icon="employee" label="Total employees" value={employees.length} detail={`${activeEmployees} active · ${employees.length - activeEmployees} inactive`} />
                    <MetricCard color={colors.employee} icon="employee" label="Active employees" value={activeEmployees} detail={`${percent(activeEmployees, employees.length)}% of employee roster`} />
                    <MetricCard color={colors.submitted} icon="work" label="Tasks submitted" value={rangeEntries.length} detail={selectedCandidate ? `By ${candidateName(selectedCandidate)}` : 'Across all candidates'} />
                    <MetricCard color={colors.reviewed} icon="review" label="Tasks reviewed" value={reviewedCount} detail={`${percent(reviewedCount, rangeEntries.length)}% of selected tasks`} />
                </Box>

                <Box className="overview-chart-grid">
                    <DonutCard
                        title="Candidate status"
                        subtitle="Current roster · active and inactive"
                        segments={[
                            { label: 'Active', value: activeCandidates, color: colors.active },
                            { label: 'Inactive', value: candidates.length - activeCandidates, color: colors.inactive },
                        ]}
                    />
                    <DonutCard
                        title="Employee status"
                        subtitle="Current roster · active and inactive"
                        segments={[
                            { label: 'Active', value: activeEmployees, color: colors.employee },
                            { label: 'Inactive', value: employees.length - activeEmployees, color: colors.inactive },
                        ]}
                    />
                    <DonutCard
                        title="Candidate task reviews"
                        subtitle={selectedCandidate ? candidateName(selectedCandidate) : 'All candidates'}
                        centerLabel="tasks"
                        segments={[
                            { label: 'Approved', value: approvedCount, color: colors.active },
                            { label: 'Rejected', value: rejectedCount, color: colors.inactive },
                            { label: 'Pending', value: pendingCount, color: colors.pending },
                        ]}
                    />
                    <DonutCard
                        title="Interview qualification"
                        subtitle="Current candidate roster"
                        centerLabel="candidates"
                        segments={[
                            { label: 'Pending review', value: pendingInterviewCount, color: colors.pending },
                            { label: 'Interview qualified', value: qualifiedInterviewCount, color: colors.active },
                            { label: 'Not qualified', value: notQualifiedInterviewCount, color: colors.inactive },
                        ]}
                    />
                    <TaskTrendCard entries={candidateEntries} from={filters.dateFrom} to={filters.dateTo} />
                </Box>

                <Box className="overview-roster-grid">
                    <RosterCard
                        title="Candidate activity"
                        subtitle={`${visibleCandidates.length} candidate${visibleCandidates.length === 1 ? '' : 's'} · submitted tasks in selected dates`}
                        emptyText="No candidate profiles to display."
                    >
                        {visibleCandidates.map(candidate => {
                            const ownEntries = rangeEntries.filter(entry => String(entry.candidateId) === String(candidate.id));
                            return <Box className="overview-roster-row" key={candidate.id}>
                                <ProfileAvatar id={candidate.id} name={candidateName(candidate)} role="Candidate" size={42} token={session.accessToken} editable onError={reportPhotoError} />
                                <Box className="overview-roster-identity">
                                    <Typography fontWeight={700} noWrap>{candidateName(candidate)}</Typography>
                                    <Typography color="text.secondary" noWrap variant="caption">{candidate.stack || candidate.username || 'Candidate'}</Typography>
                                </Box>
                                <Chip color={candidate.isActive ? 'success' : 'default'} label={candidate.isActive ? 'Active' : 'Inactive'} size="small" />
                                <Box className="overview-roster-counts">
                                    <RosterCount label="Tasks" value={ownEntries.length} />
                                    <RosterCount label="Approved" value={ownEntries.filter(entry => entry.status === 'Approved').length} />
                                    <RosterCount label="Rejected" value={ownEntries.filter(entry => entry.status === 'Rejected').length} />
                                    <RosterCount label="Pending" value={ownEntries.filter(entry => entry.status === 'Pending').length} />
                                </Box>
                            </Box>;
                        })}
                    </RosterCard>
                    <RosterCard title="Employee team" subtitle={`${employees.length} employee${employees.length === 1 ? '' : 's'} · mentor roster`} emptyText="No employee accounts have been created yet.">
                        {employees.map(employee => <Box className="overview-roster-row" key={employee.id}>
                            <ProfileAvatar id={employee.id} name={employee.displayName} role="Employee" size={42} token={session.accessToken} editable onError={reportPhotoError} />
                            <Box className="overview-roster-identity">
                                <Typography fontWeight={700} noWrap>{employee.displayName}</Typography>
                                <Typography color="text.secondary" noWrap variant="caption">{employee.department || employee.stack || 'Employee mentor'}</Typography>
                            </Box>
                            <Chip color={employee.isActive ? 'success' : 'default'} label={employee.isActive ? 'Active' : 'Inactive'} size="small" />
                        </Box>)}
                    </RosterCard>
                </Box>
                <Typography color="text.secondary" className="overview-local-photo-note" variant="caption">
                    Profile pictures are saved with their accounts and are available across devices.
                </Typography>
            </>}
        </Stack>
    );
}

function percent(part, total) {
    return total ? Math.round((part / total) * 100) : 0;
}

function MetricCard({ color, icon, label, value, detail }) {
    return <Card className="overview-metric-card" elevation={0} sx={{ '--metric-accent': color }}>
        <CardContent>
            <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={1}>
                <Typography color="text.secondary" variant="body2">{label}</Typography>
                <Box className="overview-metric-icon"><PortalIcon name={icon} /></Box>
            </Stack>
            <Typography className="overview-metric-value" component="p" variant="h3">{value}</Typography>
            <Typography color="text.secondary" noWrap variant="caption">{detail}</Typography>
        </CardContent>
    </Card>;
}

function DonutCard({ title, subtitle, segments, centerLabel = 'people' }) {
    const total = segments.reduce((sum, segment) => sum + segment.value, 0);
    const circumference = 2 * Math.PI * 64;
    return <Card className="overview-chart-card" elevation={0}>
        <CardContent>
            <Typography component="h3" fontWeight={750} variant="h6">{title}</Typography>
            <Typography color="text.secondary" variant="body2">{subtitle}</Typography>
            <Box className="overview-donut-layout">
                <Box className="overview-donut" role="img" aria-label={`${title}: ${segments.map(item => `${item.label} ${item.value}`).join(', ')}. Total ${total}.`}>
                    <svg aria-hidden="true" viewBox="0 0 160 160">
                        <circle className="overview-donut-track" cx="80" cy="80" r="64" />
                        {segments.map((segment, index) => {
                            const length = total ? (segment.value / total) * circumference : 0;
                            const segmentOffset = segments.slice(0, index).reduce(
                                (sum, previous) => sum + (total ? (previous.value / total) * circumference : 0),
                                0,
                            );
                            return <circle
                                cx="80" cy="80" key={segment.label} r="64"
                                stroke={segment.color}
                                strokeDasharray={`${length} ${circumference - length}`}
                                strokeDashoffset={-segmentOffset}
                                className="overview-donut-segment"
                            />;
                        })}
                    </svg>
                    <Box className="overview-donut-center"><Typography component="strong" variant="h4">{total}</Typography><Typography color="text.secondary" variant="caption">{centerLabel}</Typography></Box>
                </Box>
                <Stack className="overview-donut-legend" spacing={1.25}>
                    {segments.map(segment => <Stack alignItems="center" direction="row" justifyContent="space-between" key={segment.label} spacing={1}>
                        <Stack alignItems="center" direction="row" spacing={1}><Box className="overview-legend-swatch" sx={{ bgcolor: segment.color }} /><Typography variant="body2">{segment.label}</Typography></Stack>
                        <Typography fontWeight={700} variant="body2">{segment.value} <Typography component="span" color="text.secondary" variant="caption">({percent(segment.value, total)}%)</Typography></Typography>
                    </Stack>)}
                </Stack>
            </Box>
        </CardContent>
    </Card>;
}

function TaskTrendCard({ entries, from, to }) {
    const buckets = useMemo(() => {
        const start = new Date(`${from}T00:00:00`);
        const end = new Date(`${to}T00:00:00`);
        const days = Math.max(1, Math.floor((end - start) / 86400000) + 1);
        const bucketDays = Math.max(1, Math.ceil(days / 14));
        const count = Math.ceil(days / bucketDays);
        const results = Array.from({ length: count }, (_, index) => {
            const bucketStart = new Date(start);
            bucketStart.setDate(start.getDate() + index * bucketDays);
            return { label: bucketStart.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }), submitted: 0, reviewed: 0 };
        });

        for (const entry of entries) {
            if (withinRange(entry.workDate, from, to)) {
                const index = Math.floor((new Date(`${entry.workDate}T00:00:00`) - start) / 86400000 / bucketDays);
                if (results[index]) results[index].submitted += 1;
            }
            if (entry.reviewedAtUtc) {
                const reviewDate = new Date(entry.reviewedAtUtc).toISOString().slice(0, 10);
                if (withinRange(reviewDate, from, to)) {
                    const index = Math.floor((new Date(`${reviewDate}T00:00:00`) - start) / 86400000 / bucketDays);
                    if (results[index]) results[index].reviewed += 1;
                }
            }
        }
        return results;
    }, [entries, from, to]);
    const max = Math.max(1, ...buckets.flatMap(bucket => [bucket.submitted, bucket.reviewed]));
    const chartHeight = 150;
    const groupWidth = 600 / buckets.length;
    const barWidth = Math.min(13, groupWidth * 0.28);
    const anyData = buckets.some(bucket => bucket.submitted || bucket.reviewed);

    return <Card className="overview-chart-card overview-trend-card" elevation={0}>
        <CardContent>
            <Typography component="h3" fontWeight={750} variant="h6">Task activity over time</Typography>
            <Typography color="text.secondary" variant="body2">Submitted tasks and reviews by event date</Typography>
            {!anyData ? <Typography className="overview-chart-empty" color="text.secondary">No task activity in this date range.</Typography> : <>
                <Box className="overview-trend-chart" role="img" aria-label={`Task activity from ${from} to ${to}. ${buckets.reduce((sum, bucket) => sum + bucket.submitted, 0)} submitted and ${buckets.reduce((sum, bucket) => sum + bucket.reviewed, 0)} reviewed.`}>
                    <svg preserveAspectRatio="none" viewBox="0 0 600 205">
                        {[0, 1, 2, 3].map(line => <line key={line} x1="0" x2="600" y1={20 + line * 42} y2={20 + line * 42} className="overview-trend-gridline" />)}
                        {buckets.map((bucket, index) => {
                            const center = index * groupWidth + groupWidth / 2;
                            const submittedHeight = (bucket.submitted / max) * chartHeight;
                            const reviewedHeight = (bucket.reviewed / max) * chartHeight;
                            return <g key={`${bucket.label}-${index}`}>
                                <rect fill={colors.submitted} height={submittedHeight} rx="4" width={barWidth} x={center - barWidth - 2} y={170 - submittedHeight} />
                                <rect fill={colors.reviewed} height={reviewedHeight} rx="4" width={barWidth} x={center + 2} y={170 - reviewedHeight} />
                                <text className="overview-trend-label" textAnchor="middle" x={center} y="196">{bucket.label}</text>
                            </g>;
                        })}
                    </svg>
                </Box>
                <Stack className="overview-trend-legend" direction="row" spacing={2}>
                    <LegendItem color={colors.submitted} label="Submitted" />
                    <LegendItem color={colors.reviewed} label="Reviewed" />
                </Stack>
            </>}
        </CardContent>
    </Card>;
}

function LegendItem({ color, label }) {
    return <Stack alignItems="center" direction="row" spacing={0.75}><Box className="overview-legend-swatch" sx={{ bgcolor: color }} /><Typography color="text.secondary" variant="caption">{label}</Typography></Stack>;
}

function RosterCard({ title, subtitle, emptyText, children }) {
    const hasChildren = Array.isArray(children) ? children.length > 0 : Boolean(children);
    return <Card className="overview-roster-card" elevation={0}>
        <CardContent>
            <Typography component="h3" fontWeight={750} variant="h6">{title}</Typography>
            <Typography color="text.secondary" variant="body2">{subtitle}</Typography>
            <Box className="overview-roster-list">{hasChildren ? children : <Typography className="overview-roster-empty" color="text.secondary">{emptyText}</Typography>}</Box>
        </CardContent>
    </Card>;
}

function RosterCount({ label, value }) {
    return <Box className="overview-roster-count"><Typography fontWeight={750} variant="body2">{value}</Typography><Typography color="text.secondary" variant="caption">{label}</Typography></Box>;
}