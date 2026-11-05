import { useCallback, useEffect, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, CircularProgress, Dialog, DialogActions,
    DialogContent, DialogTitle, Stack, Table, TableBody, TableCell, TableContainer,
    TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';

const initialValues = { username: '', password: '', displayName: '', department: '', stack: '' };

export function EmployeeManagement({ session }) {
    const [employees, setEmployees] = useState([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');

    const loadEmployees = useCallback(async (signal) => {
        try {
            setEmployees(await apiRequest('/api/admin/employees', { token: session.accessToken, signal }));
            setError('');
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load employees.');
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.resolve().then(() => loadEmployees(controller.signal));
        return () => controller.abort();
    }, [loadEmployees]);

    async function createEmployee(values, { resetForm }) {
        setSaving(true);
        setError('');
        try {
            await apiRequest('/api/admin/employees', {
                token: session.accessToken,
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ...values, role: 'Employee' }),
            });
            resetForm();
            setDialogOpen(false);
            setNotice('Employee account created. They can now sign in as a mentor.');
            await loadEmployees();
        } catch (requestError) {
            setError(requestError.message || 'Could not create the employee account.');
        } finally {
            setSaving(false);
        }
    }

    return (
        <Stack spacing={2}>
            <Card elevation={0} sx={{ background: theme => `linear-gradient(120deg, ${theme.palette.primary.main}12, ${theme.palette.success.main}12)`, border: '1px solid', borderColor: 'divider', transition: 'transform .2s, box-shadow .2s', '&:hover': { transform: 'translateY(-2px)', boxShadow: 4 } }}>
                <CardContent>
                    <Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                        <Box sx={{ flexGrow: 1 }}>
                            <Stack alignItems="center" direction="row" spacing={1}>
                                <PortalIcon name="employee" />
                                <Typography component="h2" variant="h5">Employee mentors</Typography>
                            </Stack>
                            <Typography color="text.secondary" sx={{ mt: 0.75 }}>Create employee accounts with a department and technology stack. Employees can create and mentor their own candidates.</Typography>
                        </Box>
                        <Button onClick={() => { setError(''); setDialogOpen(true); }} startIcon={<PortalIcon name="add" />} variant="contained">Add employee</Button>
                    </Stack>
                </CardContent>
            </Card>
            {error && <Alert severity="error">{error}</Alert>}
            {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
            <Card className="content-card" elevation={0}>
                <CardContent>
                    {loading ? <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading employees...</Typography></Box> : employees.length === 0 ? (
                        <Stack alignItems="center" className="empty-state" spacing={1.5}>
                            <Box className="empty-state-icon"><PortalIcon name="employee" size={30} /></Box>
                            <Typography color="text.secondary">No employee accounts yet. Add an employee to create the first mentor.</Typography>
                        </Stack>
                    ) : <TableContainer><Table size="small" aria-label="Employee mentors">
                        <TableHead><TableRow><TableCell>Name</TableCell><TableCell>Username</TableCell><TableCell>Department</TableCell><TableCell>Technology stack</TableCell><TableCell>Role</TableCell></TableRow></TableHead>
                        <TableBody>{employees.map(employee => <TableRow hover key={employee.id}>
                            <TableCell>{employee.displayName}</TableCell><TableCell>{employee.username}</TableCell>
                            <TableCell>{employee.department}</TableCell><TableCell>{employee.stack}</TableCell><TableCell>Employee / Mentor</TableCell>
                        </TableRow>)}</TableBody>
                    </Table></TableContainer>}
                </CardContent>
            </Card>
            <Dialog fullWidth maxWidth="sm" onClose={() => !saving && setDialogOpen(false)} open={dialogOpen}>
                <Formik
                    initialValues={initialValues}
                    onSubmit={createEmployee}
                    validate={values => {
                        const errors = {};
                        if (values.username.trim().length < 3 || values.username.length > 50) errors.username = 'Use 3 to 50 characters.';
                        if (values.password.length < 12 || values.password.length > 128) errors.password = 'Use 12 to 128 characters.';
                        if (!values.displayName.trim()) errors.displayName = 'Enter the employee name.';
                        if (!values.department.trim()) errors.department = 'Enter a department.';
                        if (!values.stack.trim()) errors.stack = 'Enter a technology stack.';
                        return errors;
                    }}
                >
                    {formik => <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                        <DialogTitle>Create employee mentor</DialogTitle>
                        <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                            <TextField autoComplete="off" error={Boolean(formik.touched.username && formik.errors.username)} helperText={formik.touched.username && formik.errors.username} label="Username" name="username" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.username} />
                            <TextField autoComplete="new-password" error={Boolean(formik.touched.password && formik.errors.password)} helperText={formik.touched.password && formik.errors.password || 'Initial password: at least 12 characters.'} label="Initial password" name="password" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="password" value={formik.values.password} />
                            <TextField error={Boolean(formik.touched.displayName && formik.errors.displayName)} helperText={formik.touched.displayName && formik.errors.displayName} label="Employee name" name="displayName" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.displayName} />
                            <TextField error={Boolean(formik.touched.department && formik.errors.department)} helperText={formik.touched.department && formik.errors.department} label="Department" name="department" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.department} />
                            <TextField error={Boolean(formik.touched.stack && formik.errors.stack)} helperText={formik.touched.stack && formik.errors.stack} label="Technology stack" name="stack" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.stack} />
                            <Typography color="text.secondary" variant="body2">The account role is Employee. Employee accounts can mentor candidates but cannot create other employees.</Typography>
                        </Stack></DialogContent>
                        <DialogActions><Button disabled={saving} onClick={() => setDialogOpen(false)}>Cancel</Button><Button disabled={saving} type="submit" variant="contained">{saving ? 'Creating...' : 'Create employee'}</Button></DialogActions>
                    </Box>}
                </Formik>
            </Dialog>
        </Stack>
    );
}
