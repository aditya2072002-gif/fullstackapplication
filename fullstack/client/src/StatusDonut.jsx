import { Box, Stack, Typography } from '@mui/material';

const palette = ['#1976d2', '#d32f2f', '#ed6c02', '#2e7d32', '#7b1fa2'];

export function StatusDonut({ title, data }) {
    const total = data.reduce((sum, item) => sum + item.value, 0);
    let cursor = 0;
    const segments = data.map((item, index) => {
        const start = cursor;
        cursor += total === 0 ? 0 : (item.value / total) * 100;
        return `${palette[index % palette.length]} ${start}% ${cursor}%`;
    });
    const background = total === 0 ? '#bdbdbd' : `conic-gradient(${segments.join(', ')})`;

    return (
        <Box className="status-donut-card" role="img" aria-label={`${title}: ${data.map(item => `${item.label} ${item.value}`).join(', ')}`}>
            <Typography component="h3" variant="h6">{title}</Typography>
            <Stack alignItems="center" direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mt: 1.5 }}>
                <Box sx={{ alignItems: 'center', background: background, borderRadius: '50%', display: 'flex', height: 126, justifyContent: 'center', minWidth: 126, position: 'relative', '&::after': { backgroundColor: 'background.paper', borderRadius: '50%', content: '""', height: 82, position: 'absolute', width: 82 } }}>
                    <Typography sx={{ zIndex: 1, fontWeight: 700 }}>{total}</Typography>
                </Box>
                <Stack spacing={0.75}>
                    {data.map((item, index) => <Stack alignItems="center" direction="row" key={item.label} spacing={1}>
                        <Box sx={{ backgroundColor: palette[index % palette.length], borderRadius: '50%', height: 10, width: 10 }} />
                        <Typography variant="body2">{item.label}: {item.value}</Typography>
                    </Stack>)}
                </Stack>
            </Stack>
        </Box>
    );
}
