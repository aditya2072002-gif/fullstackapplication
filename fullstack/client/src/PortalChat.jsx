import { useEffect, useRef, useState } from 'react';
import {
    Alert, Box, Button, CircularProgress, Fab, IconButton, Paper, Stack, TextField, Typography,
} from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';

const suggestionsByRole = {
    Admin: ['Show active employees', 'Show inactive employees', 'Show candidates with task review counts'],
    Employee: ['Show my candidates', 'Show my pending work entries'],
    Candidate: ['Show my work review status', 'Show my rejected work entries'],
};

export function PortalChat({ session }) {
    const [open, setOpen] = useState(false);
    const [message, setMessage] = useState('');
    const [messages, setMessages] = useState([]);
    const [isSending, setIsSending] = useState(false);
    const [error, setError] = useState('');
    const scrollRef = useRef(null);

    useEffect(() => {
        scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
    }, [messages, isSending]);

    async function sendMessage(event, suggestedMessage) {
        event?.preventDefault();
        const question = (suggestedMessage ?? message).trim();
        if (!question || isSending) return;

        setMessages(current => [...current, { id: crypto.randomUUID(), role: 'user', text: question }]);
        setMessage('');
        setError('');
        setIsSending(true);
        try {
            const response = await apiRequest('/api/chat/ask', {
                token: session.accessToken,
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: question }),
            });
            setMessages(current => [...current, {
                id: crypto.randomUUID(),
                role: 'assistant',
                text: response.answer,
                items: response.items || [],
            }]);
        } catch (requestError) {
            if (requestError.status !== 401) {
                setError(requestError.message || 'The assistant could not retrieve these details.');
                setMessages(current => [...current, {
                    id: crypto.randomUUID(),
                    role: 'assistant',
                    text: requestError.message || 'I could not retrieve those details. Please try a supported employee, candidate, or work-review question.',
                }]);
            }
        } finally {
            setIsSending(false);
        }
    }

    return (
        <>
            {open && <Paper
                aria-label="Work tracking assistant"
                component="section"
                elevation={12}
                sx={{
                    position: 'fixed', bottom: { xs: 84, sm: 92 }, right: { xs: 12, sm: 24 }, zIndex: theme => theme.zIndex.modal,
                    display: 'flex', flexDirection: 'column', width: { xs: 'calc(100vw - 24px)', sm: 390 },
                    height: { xs: 'min(70vh, 620px)', sm: 560 }, maxHeight: 'calc(100vh - 110px)', overflow: 'hidden',
                    border: '1px solid', borderColor: 'divider', borderRadius: 3,
                }}
            >
                <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ px: 2, py: 1.5, bgcolor: 'primary.main', color: 'primary.contrastText' }}>
                    <Box>
                        <Typography fontWeight={700}>Work tracking assistant</Typography>
                        <Typography variant="caption">Role-scoped employee, candidate, and review details</Typography>
                    </Box>
                    <IconButton aria-label="Close assistant" color="inherit" onClick={() => setOpen(false)} size="small">×</IconButton>
                </Stack>
                <Stack ref={scrollRef} spacing={1.5} sx={{ flex: 1, overflowY: 'auto', p: 1.5 }}>
                    {messages.length === 0 && <Stack spacing={1}>
                        <Typography color="text.secondary" variant="body2">Search by name to see matching names, ask to list records for details, or ask for a count to see totals only. Chat cannot create, update, or delete records.</Typography>
                        {suggestionsByRole[session.user.role]?.map(suggestion => (
                            <Button key={suggestion} onClick={event => void sendMessage(event, suggestion)} size="small" sx={{ justifyContent: 'flex-start', textTransform: 'none' }} variant="outlined">
                                {suggestion}
                            </Button>
                        ))}
                    </Stack>}
                    {messages.map(item => <Box key={item.id} sx={{ alignSelf: item.role === 'user' ? 'flex-end' : 'stretch', maxWidth: item.role === 'user' ? '88%' : '100%' }}>
                        <Paper elevation={0} sx={{ p: 1.25, bgcolor: item.role === 'user' ? 'primary.main' : 'action.hover', color: item.role === 'user' ? 'primary.contrastText' : 'text.primary', borderRadius: 2 }}>
                            <Typography sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }} variant="body2">{item.text}</Typography>
                        </Paper>
                        {item.items?.map((record, index) => <Paper key={`${record.type}-${record.title}-${index}`} elevation={0} sx={{ mt: 1, p: 1.25, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
                            <Stack direction="row" justifyContent="space-between" spacing={1}>
                                <Typography fontWeight={700} variant="body2">{record.title}</Typography>
                                <Typography color="text.secondary" noWrap variant="caption">{record.status}</Typography>
                            </Stack>
                            <Stack spacing={0.25} sx={{ mt: 0.75 }}>
                                {Object.entries(record.details || {}).map(([label, value]) => value && <Typography key={label} color="text.secondary" variant="caption"><strong>{label}:</strong> {value}</Typography>)}
                            </Stack>
                        </Paper>)}
                    </Box>)}
                    {isSending && <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}><CircularProgress size={16} /><Typography color="text.secondary" variant="caption">Looking up authorized records…</Typography></Box>}
                    {error && <Alert onClose={() => setError('')} severity="error">{error}</Alert>}
                </Stack>
                <Box component="form" onSubmit={sendMessage} sx={{ p: 1.5, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Stack direction="row" spacing={1}>
                        <TextField autoComplete="off" fullWidth inputProps={{ maxLength: 500 }} label="Ask a question" onChange={event => setMessage(event.target.value)} size="small" value={message} />
                        <Button disabled={isSending || !message.trim()} type="submit" variant="contained">Send</Button>
                    </Stack>
                </Box>
            </Paper>}
            <Fab aria-label={open ? 'Close assistant' : 'Open assistant'} color="primary" onClick={() => setOpen(current => !current)} sx={{ position: 'fixed', right: { xs: 16, sm: 24 }, bottom: { xs: 16, sm: 24 }, zIndex: theme => theme.zIndex.modal }}>
                <PortalIcon name={open ? 'empty' : 'chat'} size={24} />
            </Fab>
        </>
    );
}
