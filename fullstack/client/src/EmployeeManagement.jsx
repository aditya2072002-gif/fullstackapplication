import { useCallback, useEffect, useMemo, useState } from 'react';
import { Formik } from 'formik';
import {
    Alert, Box, Button, Card, CardContent, Checkbox, CircularProgress, Dialog, DialogActions,
    DialogContent, DialogTitle, FormControl, FormControlLabel, InputLabel, MenuItem, Select,
    Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';
import { ProfileAvatar } from './ProfileAvatar.jsx';
import { StatusDonut } from './StatusDonut.jsx';

const permissionCodes = {
    viewTeam: 'Team.View', createEmployees: 'Team.CreateEmployees', manageEmployees: 'Team.ManageEmployees',
    deleteEmployees: 'Team.DeleteEmployees', createRoles: 'Team.CreateRoles', grantPermissions: 'Team.GrantPermissions',
    manageRolePermissions: 'Team.ManageRolePermissions',
};
const emptyValues = { username: '', password: '', displayName: '', department: '', stack: '' };

export function EmployeeManagement({ session, managedByEmployee = false }) {
    const [employees, setEmployees] = useState([]);
    const [roles, setRoles] = useState([]);
    const [access, setAccess] = useState({ permissions: [], catalog: [] });
    const [summary, setSummary] = useState({ activeEmployees: 0, inactiveEmployees: 0 });
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [passwordReceipt, setPasswordReceipt] = useState(null);
    const [roleDialogOpen, setRoleDialogOpen] = useState(false);
    const [permissionsDialogOpen, setPermissionsDialogOpen] = useState(false);
    const [roleName, setRoleName] = useState('');
    const [selectedPermissionRoleId, setSelectedPermissionRoleId] = useState('');
    const [rolePermissions, setRolePermissions] = useState([]);
    const [selectedRoles, setSelectedRoles] = useState({});
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const [photoError, setPhotoError] = useState('');
    const isAdmin = session.user.role === 'Admin';
    const employeesUrl = managedByEmployee ? '/api/employee/managed-employees' : '/api/admin/employees';
    const can = code => isAdmin || access.permissions.includes(code);
    const canCreateEmployees = can(permissionCodes.createEmployees);
    const canManageEmployees = can(permissionCodes.manageEmployees);
    const canDeleteEmployees = can(permissionCodes.deleteEmployees);
    const canGrant = can(permissionCodes.grantPermissions);
    const canCreateRoles = can(permissionCodes.createRoles);
    const canManageRolePermissions = can(permissionCodes.manageRolePermissions);
    const canViewTeam = !managedByEmployee || [permissionCodes.viewTeam, permissionCodes.createEmployees, permissionCodes.manageEmployees, permissionCodes.deleteEmployees, permissionCodes.createRoles, permissionCodes.grantPermissions, permissionCodes.manageRolePermissions].some(can);
    const chartData = useMemo(() => [
        { label: 'Active', value: summary.activeEmployees },
        { label: 'Inactive', value: summary.inactiveEmployees },
    ], [summary]);

    const loadEmployees = useCallback(async signal => {
        try {
            const employeeData = await apiRequest(employeesUrl, { token: session.accessToken, signal });
            setEmployees(employeeData);
            setSelectedRoles(Object.fromEntries(employeeData.map(employee => [employee.id, String(employee.roleDefinitionId ?? '')])));
            setError('');
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load employees.');
        } finally {
            if (!signal?.aborted) setLoading(false);
        }
    }, [employeesUrl, session.accessToken]);

    const loadRoles = useCallback(async signal => {
        try {
            const [accessData, roleData] = await Promise.all([
                apiRequest('/api/roles/permissions', { token: session.accessToken, signal }),
                canGrant || canManageRolePermissions ? apiRequest('/api/roles', { token: session.accessToken, signal }) : Promise.resolve([]),
            ]);
            setAccess(accessData);
            setRoles(roleData);
        } catch (requestError) {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load employee permissions.');
        }
    }, [canGrant, canManageRolePermissions, session.accessToken]);

    useEffect(() => {
        const controller = new AbortController();
        Promise.all([
            apiRequest(employeesUrl, { token: session.accessToken, signal: controller.signal }),
            apiRequest('/api/roles/permissions', { token: session.accessToken, signal: controller.signal }),
            canGrant || canManageRolePermissions ? apiRequest('/api/roles', { token: session.accessToken, signal: controller.signal }) : Promise.resolve([]),
        ]).then(([employeeData, accessData, roleData]) => {
            setEmployees(employeeData);
            setSelectedRoles(Object.fromEntries(employeeData.map(employee => [employee.id, String(employee.roleDefinitionId ?? '')])));
            setAccess(accessData);
            setRoles(roleData);
            setError('');
        }).catch(requestError => {
            if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load employee access.');
        }).finally(() => {
            if (!controller.signal.aborted) setLoading(false);
        });
        return () => controller.abort();
    }, [canGrant, canManageRolePermissions, employeesUrl, session.accessToken]);

    useEffect(() => {
        if (!managedByEmployee || !access.permissions.includes(permissionCodes.viewTeam)) return undefined;
        const controller = new AbortController();
        apiRequest(`${employeesUrl}/summary`, { token: session.accessToken, signal: controller.signal })
            .then(setSummary)
            .catch(requestError => { if (requestError.name !== 'AbortError') setError(requestError.message || 'Could not load employee status counts.'); });
        return () => controller.abort();
    }, [access.permissions, employeesUrl, managedByEmployee, session.accessToken]);

    async function createEmployee(values, { resetForm }) {
        setSaving(true);
        setError('');
        try {
            await apiRequest(employeesUrl, {
                token: session.accessToken,
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(values),
            });
            setPasswordReceipt({ username: values.username, password: values.password });
            resetForm();
            setDialogOpen(false);
            setNotice('Employee account created without an assigned permission role. Save the initial password now; it will not be shown again.');
            await loadEmployees();
        } catch (requestError) {
            setError(requestError.message || 'Could not create the employee account.');
        } finally {
            setSaving(false);
        }
    }

    async function updateRole(employeeId, roleDefinitionId) {
        setError('');
        try {
            await apiRequest(`/api/roles/employees/${employeeId}`, {
                token: session.accessToken, method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ roleDefinitionId: roleDefinitionId ? Number(roleDefinitionId) : null }),
            });
            setNotice('Employee access role updated.');
            await loadEmployees();
        } catch (requestError) {
            setError(requestError.message || 'Could not update employee access.');
        }
    }

    async function toggleEmployeeStatus(employee) {
        setError('');
        try {
            const route = managedByEmployee ? employeesUrl : '/api/admin/employees';
            await apiRequest(`${route}/${employee.id}/status`, {
                token: session.accessToken, method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ isActive: !employee.isActive }),
            });
            setNotice(employee.isActive ? 'Employee access deactivated.' : 'Employee access reactivated.');
            await loadEmployees();
        } catch (requestError) {
            setError(requestError.message || 'Could not update employee status.');
        }
    }

    async function deleteEmployee(employee) {
        if (employee.isActive || !window.confirm(`Permanently delete inactive employee ${employee.displayName}?`)) return;
        setError('');
        try {
            const route = managedByEmployee ? employeesUrl : '/api/admin/employees';
            await apiRequest(`${route}/${employee.id}`, { token: session.accessToken, method: 'DELETE' });
            setNotice('Inactive employee account permanently deleted.');
            await loadEmployees();
        } catch (requestError) {
            setError(requestError.message || 'Could not delete the employee account.');
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

    async function createRole(event) {
        event.preventDefault();
        setSaving(true);
        setError('');
        try {
            await apiRequest('/api/roles', {
                token: session.accessToken, method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: roleName }),
            });
            setRoleName('');
            setRoleDialogOpen(false);
            setNotice('Role created without permissions. Manage its permissions separately before assigning it.');
            await loadRoles();
        } catch (requestError) {
            setError(requestError.message || 'Could not create the access role.');
        } finally {
            setSaving(false);
        }
    }

    async function saveRolePermissions(event) {
        event.preventDefault();
        setSaving(true);
        setError('');
        try {
            await apiRequest(`/api/roles/${selectedPermissionRoleId}/permissions`, {
                token: session.accessToken, method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ permissions: rolePermissions }),
            });
            setPermissionsDialogOpen(false);
            setNotice('Role permissions updated.');
            await loadRoles();
        } catch (requestError) {
            setError(requestError.message || 'Could not update role permissions.');
        } finally {
            setSaving(false);
        }
    }

    function selectPermissionRole(roleId) {
        setSelectedPermissionRoleId(roleId);
        const selectedRole = roles.find(role => String(role.id) === roleId);
        setRolePermissions(selectedRole?.permissions ?? []);
    }

    return (
        <Stack spacing={2}>
            <Card elevation={0} sx={{ background: theme => `linear-gradient(120deg, ${theme.palette.primary.main}12, ${theme.palette.success.main}12)`, border: '1px solid', borderColor: 'divider' }}>
                <CardContent><Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <Box sx={{ flexGrow: 1 }}>
                        <Stack alignItems="center" direction="row" spacing={1}><PortalIcon name="employee" /><Typography component="h2" variant="h5">{managedByEmployee ? 'My employee team' : 'Employee access management'}</Typography></Stack>
                        <Typography color="text.secondary" sx={{ mt: 0.75 }}>Create employee accounts, create roles, manage role permissions, and assign roles as separate actions.</Typography>
                    </Box>
                    {canCreateRoles && <Button onClick={() => { setError(''); setRoleDialogOpen(true); }} variant="outlined">Create role</Button>}
                    {canManageRolePermissions && <Button onClick={() => { setError(''); selectPermissionRole(''); setPermissionsDialogOpen(true); }} disabled={!roles.length} variant="outlined">Manage role permissions</Button>}
                    {canCreateEmployees && <Button onClick={() => { setError(''); setDialogOpen(true); }} startIcon={<PortalIcon name="add" />} variant="contained">Add employee</Button>}
                </Stack></CardContent>
            </Card>
            {error && <Alert severity="error">{error}</Alert>}
            {photoError && <Alert onClose={() => setPhotoError('')} severity="error">{photoError}</Alert>}
            {notice && <Alert onClose={() => setNotice('')} severity="success">{notice}</Alert>}
            {managedByEmployee && canViewTeam && <Card className="content-card" elevation={0}><CardContent><StatusDonut title="Direct reports by status" data={chartData} /></CardContent></Card>}
            <Card className="content-card" elevation={0}><CardContent>
                {loading ? <Box className="loading-state"><CircularProgress size={26} /><Typography>Loading employees...</Typography></Box> : employees.length === 0 ? (
                    <Stack alignItems="center" className="empty-state" spacing={1.5}><Box className="empty-state-icon"><PortalIcon name="employee" size={30} /></Box><Typography color="text.secondary">No employees are assigned to your management scope.</Typography></Stack>
                ) : <TableContainer><Table size="small" aria-label="Managed employees"><TableHead><TableRow>
                    {(canGrant || canManageEmployees || canDeleteEmployees) && <TableCell>Access actions</TableCell>}
                </TableRow></TableHead><TableBody>{employees.map(employee => <TableRow hover key={employee.id}>
                    <TableCell><Stack alignItems="center" direction="row" spacing={1.25}><ProfileAvatar id={employee.id} name={employee.displayName} onError={setPhotoError} role="Employee" size={36} token={session.accessToken} editable />{employee.displayName}</Stack></TableCell>
                    <TableCell>{employee.username}</TableCell><TableCell>{employee.department}</TableCell><TableCell>{employee.stack}</TableCell><TableCell>{employee.roleDefinitionName || 'No permissions'}</TableCell>
                    <TableCell>{employee.isActive ? 'Active' : 'Inactive'}</TableCell>
                    {(canGrant || canManageEmployees || canDeleteEmployees) && <TableCell><Stack direction="row" spacing={1} alignItems="center">
                        {canGrant && <><FormControl size="small" sx={{ minWidth: 140 }}><InputLabel id={`role-${employee.id}`}>Role</InputLabel><Select label="Role" labelId={`role-${employee.id}`} onChange={event => setSelectedRoles(current => ({ ...current, [employee.id]: event.target.value }))} value={selectedRoles[employee.id] ?? ''}><MenuItem value="">No permissions</MenuItem>{roles.map(role => <MenuItem key={role.id} value={String(role.id)}>{role.name}</MenuItem>)}</Select></FormControl><Button disabled={selectedRoles[employee.id] === String(employee.roleDefinitionId ?? '')} onClick={() => void updateRole(employee.id, selectedRoles[employee.id])} size="small">Apply</Button></>}
                        {canManageEmployees && <Button color={employee.isActive ? 'warning' : 'success'} onClick={() => void toggleEmployeeStatus(employee)} size="small">{employee.isActive ? 'Deactivate' : 'Reactivate'}</Button>}
                        {canDeleteEmployees && !employee.isActive && <Button color="error" onClick={() => void deleteEmployee(employee)} size="small">Delete</Button>}
                    </Stack></TableCell>}
                </TableRow>)}</TableBody></Table></TableContainer>}
            </CardContent></Card>
            <Dialog fullWidth maxWidth="sm" onClose={() => !saving && setDialogOpen(false)} open={dialogOpen}>
                <Formik initialValues={emptyValues} onSubmit={createEmployee} validate={values => {
                    const errors = {};
                    if (values.username.trim().length < 3 || values.username.length > 50) errors.username = 'Use 3 to 50 characters.';
                    if (values.password.length < 12 || values.password.length > 128) errors.password = 'Use 12 to 128 characters.';
                    if (!values.displayName.trim()) errors.displayName = 'Enter the employee name.';
                    if (!values.department.trim()) errors.department = 'Enter a department.';
                    if (!values.stack.trim()) errors.stack = 'Enter a technology stack.';
                    return errors;
                }}>
                    {formik => <Box component="form" noValidate onSubmit={formik.handleSubmit}>
                        <DialogTitle>Create employee account</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                            <TextField autoComplete="off" error={Boolean(formik.touched.username && formik.errors.username)} helperText={formik.touched.username && formik.errors.username} label="Username" name="username" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.username} />
                            <TextField autoComplete="new-password" error={Boolean(formik.touched.password && formik.errors.password)} helperText={formik.touched.password && formik.errors.password || 'Initial password: at least 12 characters.'} label="Initial password" name="password" onBlur={formik.handleBlur} onChange={formik.handleChange} required type="password" value={formik.values.password} />
                            <TextField error={Boolean(formik.touched.displayName && formik.errors.displayName)} helperText={formik.touched.displayName && formik.errors.displayName} label="Employee name" name="displayName" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.displayName} />
                            <TextField error={Boolean(formik.touched.department && formik.errors.department)} helperText={formik.touched.department && formik.errors.department} label="Department" name="department" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.department} />
                            <TextField error={Boolean(formik.touched.stack && formik.errors.stack)} helperText={formik.touched.stack && formik.errors.stack} label="Technology stack" name="stack" onBlur={formik.handleBlur} onChange={formik.handleChange} required value={formik.values.stack} />
                            <Typography color="text.secondary" variant="body2">Role assignment is a separate action available after the employee account is created.</Typography>
                        </Stack></DialogContent><DialogActions><Button disabled={saving} onClick={() => setDialogOpen(false)}>Cancel</Button><Button disabled={saving} type="submit" variant="contained">{saving ? 'Creating...' : 'Create employee'}</Button></DialogActions>
                    </Box>}
                </Formik>
            </Dialog>
            <Dialog onClose={() => { setPasswordReceipt(null); setNotice(''); }} open={Boolean(passwordReceipt)}>
                <DialogTitle>Employee account created</DialogTitle>
                <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
                    <Typography>Share this initial password securely with {passwordReceipt?.username}. It is shown only once and cannot be retrieved later.</Typography>
                    <TextField InputProps={{ readOnly: true }} label="Initial password" value={passwordReceipt?.password ?? ''} />
                </Stack></DialogContent>
                <DialogActions><Button onClick={() => void copyInitialPassword()}>Copy password</Button><Button onClick={() => setPasswordReceipt(null)} variant="contained">Done</Button></DialogActions>
            </Dialog>
            <Dialog fullWidth maxWidth="sm" onClose={() => !saving && setRoleDialogOpen(false)} open={roleDialogOpen}>
                <Box component="form" onSubmit={createRole}><DialogTitle>Create role</DialogTitle><DialogContent><Stack spacing={1} sx={{ pt: 1 }}>
                    <TextField label="Role name" onChange={event => setRoleName(event.target.value)} required value={roleName} />
                    <Typography color="text.secondary" variant="body2">Role permissions are managed separately after the role is created.</Typography>
                </Stack></DialogContent><DialogActions><Button disabled={saving} onClick={() => setRoleDialogOpen(false)}>Cancel</Button><Button disabled={saving || roleName.trim().length < 2} type="submit" variant="contained">{saving ? 'Saving...' : 'Create role'}</Button></DialogActions></Box>
            </Dialog>
            <Dialog fullWidth maxWidth="sm" onClose={() => !saving && setPermissionsDialogOpen(false)} open={permissionsDialogOpen}>
                <Box component="form" onSubmit={saveRolePermissions}><DialogTitle>Manage role permissions</DialogTitle><DialogContent><Stack spacing={1} sx={{ pt: 1 }}>
                    <FormControl fullWidth><InputLabel id="permissions-role-label">Role</InputLabel><Select label="Role" labelId="permissions-role-label" onChange={event => selectPermissionRole(event.target.value)} value={selectedPermissionRoleId}><MenuItem value=""><em>Select role</em></MenuItem>{roles.map(role => <MenuItem key={role.id} value={String(role.id)}>{role.name}</MenuItem>)}</Select></FormControl>
                    {selectedPermissionRoleId && access.catalog.map(item => <FormControlLabel key={item.code} control={<Checkbox checked={rolePermissions.includes(item.code)} onChange={event => setRolePermissions(current => event.target.checked ? [...current, item.code] : current.filter(code => code !== item.code))} />} label={item.label} />)}
                </Stack></DialogContent><DialogActions><Button disabled={saving} onClick={() => setPermissionsDialogOpen(false)}>Cancel</Button><Button disabled={saving || !selectedPermissionRoleId} type="submit" variant="contained">{saving ? 'Saving...' : 'Save permissions'}</Button></DialogActions></Box>
            </Dialog>
        </Stack>
    );
}
