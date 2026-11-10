so import { Box, Stack, Typography } from '@mui/material';

export function WorkReportChart({ title, description, descriptionScale, data, valueLabel, maxValue: scaleMax }) {
    const maxValue = scaleMax ?? Math.max(...data.map(item => item.value), 1);

    return (
        <Box className="work-report-chart" role="img" aria-label={`${title}. ${description}`}>
            <Typography component="h3" variant="h6">{title}</Typography>
            <Typography color="text.secondary" variant="body2">{description}</Typography>
            {descriptionScale && <Typography className="chart-scale" color="text.secondary" variant="caption">Scale: {descriptionScale}</Typography>}
            {data.length === 0 ? (
                <Typography className="chart-empty" color="text.secondary">No report data yet.</Typography>
            ) : (
                <Stack className="chart-bars" direction="row" alignItems="flex-end" spacing={1}>
                    {data.map((item, index) => {
                        const normalizedValue = Math.min(Math.max(Number(item.value) || 0, 0), maxValue);
                        const height = Math.max((normalizedValue / maxValue) * 100, normalizedValue > 0 ? 2 : 0);
                        return (
                            <Box className="chart-bar-column" key={`${item.label}-${index}`} title={`${item.label}: ${item.value} ${valueLabel}`}>
                                <Typography className="chart-value">{item.value}</Typography>
                                <Box className="chart-bar-track"><Box className="chart-bar" sx={{ height: `${height}%` }} /></Box>
                                <Typography className="chart-label">{item.label}</Typography>
                            </Box>
                        );
                    })}
                </Stack>
            )}
        </Box>
    );
}
