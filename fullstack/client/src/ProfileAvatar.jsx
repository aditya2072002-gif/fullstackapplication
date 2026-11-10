import { useEffect, useRef, useState } from 'react';
import { Avatar, Box, IconButton, Tooltip } from '@mui/material';
import { apiRequest } from './api.js';
import { PortalIcon } from './PortalIcon.jsx';

const photoEventName = 'profile-photo-updated';

function profilePhotoKey(role, id, username) {
    const identity = id == null ? username?.trim().toLocaleLowerCase() : String(id);
    return `work-tracking-profile-photo:${role.toLocaleLowerCase()}:${encodeURIComponent(identity || 'unknown')}`;
}

export function ProfileAvatar({ role, id, username, name, token, size = 40, editable = false, onError }) {
    const key = profilePhotoKey(role, id, username);
    const inputRef = useRef(null);
    const onErrorRef = useRef(onError);
    const [photo, setPhoto] = useState('');
    const initials = (name || username || role).split(/\s+/).slice(0, 2).map(part => part[0]).join('').toLocaleUpperCase();

    useEffect(() => {
        onErrorRef.current = onError;
    }, [onError]);

    useEffect(() => {
        let active = true;
        let objectUrl = '';
        let requestController;

        async function refreshPhoto(event) {
            if (event?.detail?.key && event.detail.key !== key) return;
            if (!token || id == null) return;
            requestController?.abort();
            requestController = new AbortController();
            try {
                const blob = await apiRequest(`/api/profile-photos/${encodeURIComponent(id)}`, {
                    token, responseType: 'blob', signal: requestController.signal,
                });
                if (!active) return;
                if (!blob?.size) {
                    let legacyPhoto = '';
                    try {
                        legacyPhoto = window.localStorage.getItem(key) || '';
                    } catch {
                        legacyPhoto = '';
                    }
                    if (legacyPhoto) {
                        const legacyBlob = await (await fetch(legacyPhoto, { signal: requestController.signal })).blob();
                        if (legacyBlob.type === 'image/jpeg' && legacyBlob.size > 0 && legacyBlob.size <= 5 * 1024 * 1024) {
                            setPhoto(legacyPhoto);
                            const formData = new FormData();
                            formData.append('photo', legacyBlob, 'profile.jpg');
                            await apiRequest(`/api/profile-photos/${encodeURIComponent(id)}`, {
                                token, method: 'PUT', body: formData, signal: requestController.signal,
                            });
                            if (active) {
                                window.localStorage.removeItem(key);
                                window.dispatchEvent(new CustomEvent(photoEventName, { detail: { key } }));
                            }
                        }
                        return;
                    }
                }
                if (objectUrl) URL.revokeObjectURL(objectUrl);
                objectUrl = blob?.size ? URL.createObjectURL(blob) : '';
                setPhoto(objectUrl);
            } catch (error) {
                if (error.name !== 'AbortError') onErrorRef.current?.(error.message || 'Could not load the profile photo.');
            }
        }

        void refreshPhoto();
        window.addEventListener(photoEventName, refreshPhoto);
        return () => {
            active = false;
            requestController?.abort();
            window.removeEventListener(photoEventName, refreshPhoto);
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [id, key, token]);

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
            const context = canvas.getContext('2d');
            if (!context) throw new Error('Could not process this image.');
            context.drawImage(image, 0, 0, canvas.width, canvas.height);
            image.close();
            const preview = canvas.toDataURL('image/jpeg', 0.82);
            const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', 0.82));
            if (!blob) throw new Error('Could not process this image.');
            const formData = new FormData();
            formData.append('photo', blob, 'profile.jpg');
            await apiRequest(`/api/profile-photos/${encodeURIComponent(id)}`, {
                token, method: 'PUT', body: formData,
            });
            setPhoto(preview);
            window.dispatchEvent(new CustomEvent(photoEventName, { detail: { key } }));
        } catch (error) {
            onError?.(error.message || 'Could not save this photo to your profile.');
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
