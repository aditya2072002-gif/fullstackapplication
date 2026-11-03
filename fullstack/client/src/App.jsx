import { useEffect, useMemo, useState } from 'react';
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Container,
    CssBaseline,
    Stack,
    TextField,
    ThemeProvider,
    Typography,
    createTheme,
} from '@mui/material';
import { AdminDashboard } from './AdminDashboard.jsx';
import { CandidatePortal } from './CandidatePortal.jsx';
import { apiRequest } from './api.js';
import './App.css';

function App() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [session, setSession] = useState(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [loginError, setLoginError] = useState('');
    const [darkMode, setDarkMode] = useState(() => localStorage.getItem('portal-dark-mode') === 'true');
    const theme = useMemo(() => createTheme({
        palette: {
            mode: darkMode ? 'dark' : 'light',
            primary: { main: darkMode ? '#91b5ed' : '#193b68' },
            background: { default: darkMode ? '#101723' : '#f4f7fc', paper: darkMode ? '#192333' : '#ffffff' },
        },
    }), [darkMode]);

    useEffect(() => {
        localStorage.setItem('portal-dark-mode', String(darkMode));
    }, [darkMode]);

    async function handleLogin(event) {
        event.preventDefault();
        setIsSubmitting(true);
        setLoginError('');

        try {
            const result = await apiRequest('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password }),
            });

            if (!['Admin', 'Candidate'].includes(result.user?.role)) {
                setLoginError('This account has an unsupported role.');
                return;
            }

            setSession(result);
            setPassword('');
        } catch (error) {
            setLoginError(error.status === 401
                ? 'Invalid username or password.'
                : error.message || 'Could not reach the server. Check that the API is running and try again.');
        } finally {
            setIsSubmitting(false);
        }
    }

    function handleLogout() {
        setSession(null);
        setLoginError('');
        setUsername('');
        setPassword('');
    }

    if (session?.user?.role === 'Admin') {
        return <ThemeProvider theme={theme}><CssBaseline /><AdminDashboard darkMode={darkMode} onDarkModeChange={setDarkMode} session={session} onLogout={handleLogout} /></ThemeProvider>;
    }

    if (session?.user?.role === 'Candidate') {
        return <ThemeProvider theme={theme}><CssBaseline /><CandidatePortal darkMode={darkMode} onDarkModeChange={setDarkMode} session={session} onLogout={handleLogout} /></ThemeProvider>;
    }

    return (
        <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box className={`app-shell login-shell${darkMode ? ' theme-dark' : ''}`}>
            <Container className="login-container" maxWidth={false}>
                <Box className="login-layout">
                    <Box component="section" className="login-showcase" aria-labelledby="login-showcase-title">
                        <Box className="login-brand">
                            <Box className="login-brand-mark" aria-hidden="true">
                                <svg fill="none" viewBox="0 0 32 32">
                                    <path d="M7 22.5 13.1 16l4.3 4.1L25 10" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" />
                                    <path d="M19.5 10H25v5.5" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" />
                                </svg>
                            </Box>
                            <Typography component="span" className="login-brand-name">Work Tracking</Typography>
                        </Box>
                        <Box className="login-showcase-copy">
                            <Typography className="login-kicker">A clearer way to move work forward</Typography>
                            <Typography id="login-showcase-title" component="h1" className="login-showcase-title">
                                Your work,<br />in good <span>focus.</span>
                            </Typography>
                            <Typography className="login-showcase-description">
                                Bring daily progress, learning, and feedback together in one workspace built for your team.
                            </Typography>
                        </Box>
                        <Box className="login-artwork" aria-hidden="true">
                            <svg className="login-artwork-lines" fill="none" viewBox="0 0 580 330">
                                <path d="M13 279c74-4 76-125 157-125 67 0 63 77 128 77 63 0 63-112 139-112 51 0 69 44 130 34" stroke="url(#login-line-gradient)" strokeDasharray="5 9" strokeLinecap="round" strokeWidth="2" />
                                <path d="M33 298c87-5 80-112 148-112 65 0 67 72 120 72 72 0 75-95 137-95 45 0 72 34 119 27" stroke="url(#login-line-gradient)" strokeLinecap="round" strokeOpacity=".38" strokeWidth="1.5" />
                                <defs><linearGradient id="login-line-gradient" x1="13" x2="567" y1="198" y2="198" gradientUnits="userSpaceOnUse"><stop stopColor="#8DD9D0" /><stop offset=".52" stopColor="#A3B5FF" /><stop offset="1" stopColor="#FFC486" /></linearGradient></defs>
                            </svg>
                            <Box className="login-orbit login-orbit-one" />
                            <Box className="login-orbit login-orbit-two" />
                            <Card className="login-insight-card" elevation={0}>
                                <Stack direction="row" alignItems="center" justifyContent="space-between">
                                    <Box><Typography className="login-insight-label">Weekly progress</Typography><Typography className="login-insight-value">On track</Typography></Box>
                                    <Box className="login-insight-indicator" aria-hidden="true">↗</Box>
                                </Stack>
                                <Box className="login-chart" aria-hidden="true">
                                    {[38, 58, 46, 74, 62, 88, 70].map((height, index) => <Box className="login-chart-bar" key={index} sx={{ height: `${height}%` }} />)}
                                </Box>
                                <Stack direction="row" justifyContent="space-between"><Typography className="login-chart-caption">Daily momentum</Typography><Typography className="login-chart-caption">This week</Typography></Stack>
                            </Card>
                            <Box className="login-artwork-spark login-artwork-spark-one" />
                            <Box className="login-artwork-spark login-artwork-spark-two" />
                        </Box>
                        <Typography className="login-showcase-footer">One workspace. Clearer progress.</Typography>
                    </Box>
                    <Card className="login-card" elevation={0}>
                        <CardContent className="login-card-content">
                            <Stack spacing={3}>
                                <Box>
                                    <Typography className="eyebrow">Welcome back</Typography>
                                    <Typography component="h2" variant="h4" className="page-title">Sign in</Typography>
                                    <Typography color="text.secondary" sx={{ mt: 1 }}>
                                        Use your account to continue to your portal.
                                    </Typography>
                                </Box>
                                <Box component="form" onSubmit={handleLogin}>
                                    <Stack spacing={2}>
                                        <TextField
                                            autoComplete="username"
                                            autoFocus
                                            fullWidth
                                            label="Username"
                                            onChange={event => setUsername(event.target.value)}
                                            required
                                            value={username}
                                        />
                                        <TextField
                                            autoComplete="current-password"
                                            fullWidth
                                            label="Password"
                                            onChange={event => setPassword(event.target.value)}
                                            required
                                            type="password"
                                            value={password}
                                        />
                                        {loginError && <Alert severity="error">{loginError}</Alert>}
                                        <Button disabled={isSubmitting} type="submit" variant="contained" size="large">
                                            {isSubmitting ? 'Signing in…' : 'Sign in'}
                                        </Button>
                                    </Stack>
                                </Box>
                            </Stack>
                        </CardContent>
                    </Card>
                </Box>
            </Container>
        </Box>
        </ThemeProvider>
    );
}

export default App;