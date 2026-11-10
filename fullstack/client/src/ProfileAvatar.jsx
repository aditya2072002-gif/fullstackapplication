import { useEffect, useRef, useState } from 'react';
import { Avatar, Box, IconButton, Tooltip } from '@mui/material';
import { PortalIcon } from './PortalIcon.jsx';

const photoEventName = 'profile-photo-updated';

function profilePhotoKey(role, id, username) {
    const identity = id == null ? username?.trim().toLocaleLowerCase() : String(id);
    return `work-tracking-profile-photo:${role.toLocaleLowerCase()}:${encodeURIComponent(identity || 'unknown')}`;
}

export function ProfileAvatar({ role, id, username, name, size = 40, editable = false, onError }) {
    const key = profilePhotoKey(role, id, username);
    const inputRef = useRef(null);
    const [photo, setPhoto] = useState(() => {
        try {
            return localStorage.getItem(key) || '';
        } catch {
            return '';
        }
    });
    const initials = (name || username || role).split(/\s+/).slice(0, 2).map(part => part[0]).join('').toLocaleUpperCase();

    useEffect(() => {
        function refreshPhoto(event) {
            if (event.type === 'storage' && event.key !== key) return;
            if (event.type === photoEventName && event.detail?.key !== key) return;
            try {
                setPhoto(localStorage.getItem(key) || '');
            } catch {
                setPhoto('');
            }
        }

        window.addEventListener('storage', refreshPhoto);
        window.addEventListener(photoEventName, refreshPhoto);
        return () => {
            window.removeEventListener('storage', refreshPhoto);
            window.removeEventListener(photoEventName, refreshPhoto);
        };
    }, [key]);

    async function handlePhotoChange(event) {
        const file = event.target.files?.[0];
        event.target.value = '';
        if (!file) return;
        if (!file.type.startsWith('image/')) {
            onError?.('Choose an image file to upload.');
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            onError?.('Profile images must be 5 MB or smaller.');
            return;
        }

        try {
            const image = await createImageBitmap(file);
            const scale = Math.min(1, 256 / Math.max(image.width, image.height));
            const canvas = document.createElement('canvas');
            canvas.width = Math.max(1, Math.round(image.width * scale));
            canvas.height = Math.max(1, Math.round(image.height * scale));
            canvas.getContext('2d').drawImage(image, 0, 0, canvas.width, canvas.height);
            image.close();
            const dataUrl = canvas.toDataURL('image/jpeg', 0.82);
            localStorage.setItem(key, dataUrl);
            setPhoto(dataUrl);
            window.dispatchEvent(new CustomEvent(photoEventName, { detail: { key } }));
        } catch {
            onError?.('Could not save this photo in this browser. Try a smaller image or free browser storage.');
        }
    }

    return (
        <Box className="profile-photo-wrap" sx={{ width: size, height: size }}>
            <Avatar alt={name || role} src={photo || undefined} sx={{ width: size, height: size, fontSize: Math.max(12, size * 0.34), fontWeight: 700 }}>
                {initials}
            </Avatar>
            {editable && <>
                <input accept="image/*" aria-label={`Choose ${role} profile photo`} className="profile-photo-input" onChange={handlePhotoChange} ref={inputRef} type="file" />
                <Tooltip title="Upload profile photo">
                    <IconButton aria-label={`Upload ${role} profile photo`} className="profile-photo-edit" onClick={event => { event.stopPropagation(); inputRef.current?.click(); }} size="small">
                        <PortalIcon name="add" size={13} />
                    </IconButton>
                </Tooltip>
            </>}
        </Box>
    );
}
