(() => {
    const container = document.querySelector('#learningMeeting');
    const status = document.querySelector('#meetingStatus');
    const retry = document.querySelector('#retryMeeting');
    let api;

    function dispose() {
        api?.dispose();
        api = undefined;
        container.replaceChildren();
    }

    function fail(message) {
        status.textContent = message;
        retry.hidden = false;
    }

    function initialize() {
        if (api) return;
        try {
            api = new window.JitsiMeetExternalAPI(container.dataset.domain, {
                roomName: container.dataset.room,
                width: '100%',
                height: '100%',
                parentNode: container,
                userInfo: { displayName: container.dataset.displayName }
            });
            api.addListener('videoConferenceJoined', () => {
                status.textContent = 'Connected to the video meeting.';
            });
            api.addListener('errorOccurred', () => fail(
                'Jitsi reported an error. Check your connection and camera or microphone permissions.'));
            api.addListener('cameraError', () => fail('Check your camera permissions or continue with audio.'));
            api.addListener('micError', () => fail('Check your microphone permissions in your browser.'));
            api.addListener('peerConnectionFailure', () => fail('Jitsi lost the media connection. Check your network.'));
            api.addListener('readyToClose', () => {
                dispose();
                status.textContent = 'You left the meeting. Return to Session Details to continue.';
                retry.hidden = true;
            });
        } catch {
            dispose();
            fail('The meeting could not be initialized.');
        }
    }

    retry.addEventListener('click', () => window.location.reload());
    window.addEventListener('pagehide', dispose);
    window.addEventListener('pageshow', event => { if (event.persisted) window.location.reload(); });
    const script = document.createElement('script');
    const timeout = window.setTimeout(() => {
        script.remove();
        fail('The video provider did not respond. Check your network and retry.');
    }, 12000);
    script.src = 'https://' + container.dataset.domain + '/external_api.js';
    script.onload = () => {
        window.clearTimeout(timeout);
        status.textContent = 'Connecting to Jitsi. Follow any moderator sign-in or waiting instructions shown below.';
        initialize();
    };
    script.onerror = () => {
        window.clearTimeout(timeout);
        fail('The video provider could not load. Check your network and retry.');
    };
    document.head.append(script);
})();
