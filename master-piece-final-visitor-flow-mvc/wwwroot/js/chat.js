(() => {
    const chat = document.querySelector('#learningChat[data-request]');
    if (!chat?.dataset.request) return;
    const id = chat.dataset.request;
    const endpoint = '/chat/' + id + '/messages';
    const list = document.querySelector('#chatMessages');
    const status = document.querySelector('#chatStatus');
    const form = document.querySelector('#chatCompose');
    const input = document.querySelector('#messageInput');
    const submit = form.querySelector('button');
    const older = document.querySelector('#chatOlder');
    const token = form.elements.__RequestVerificationToken.value;
    const selected = document.querySelector('[data-conversation="' + id + '"]');
    const rendered = new Set();
    const waitingRead = new Set();
    let latest, earliest, timer;
    let delay = 5000;
    let fetching = false;
    let sending = false;
    let canSend = !input.disabled;
    let sendError = '';

    function post(url, values) {
        values.set('__RequestVerificationToken', token);
        return fetch(url, { method: 'POST', body: values });
    }
    function show(message) { status.textContent = sendError || message; }
    function updateComposer() {
        input.disabled = !canSend;
        submit.disabled = sending || !canSend;
    }
    function render(rows, before) {
        const fragment = document.createDocumentFragment();
        for (const row of rows) {
            if (row.unread) waitingRead.add(row.id);
            if (rendered.has(row.id)) continue;
            rendered.add(row.id);
            const message = document.createElement('div');
            message.className = 'message ' + (row.mine ? 'sent' : 'received');
            const content = document.createElement('div');
            content.className = 'message-content';
            const bubble = document.createElement('div');
            bubble.className = 'message-bubble';
            bubble.textContent = row.body;
            const time = document.createElement('time');
            time.className = 'message-time';
            time.dateTime = row.utc;
            time.textContent = new Date(row.utc).toLocaleString();
            content.append(bubble, time);
            message.append(content);
            fragment.append(message);
        }
        if (before) {
            const height = list.scrollHeight;
            list.prepend(fragment);
            list.scrollTop += list.scrollHeight - height;
        } else {
            const atBottom = list.scrollHeight - list.clientHeight - list.scrollTop < 50;
            list.append(fragment);
            if (atBottom) list.scrollTop = list.scrollHeight;
        }
    }
    async function markDisplayed() {
        if (document.hidden || waitingRead.size === 0) return;
        const ids = [...waitingRead].slice(0, 40);
        const response = await post('/chat/' + id + '/read',
            new URLSearchParams(ids.map(value => ['ids', value])));
        if (response.status !== 204) return;
        ids.forEach(value => waitingRead.delete(value));
        const badge = selected.querySelector('.unread-badge');
        const count = Math.max(0, Number(badge.textContent) - ids.length);
        badge.textContent = count;
        badge.hidden = count === 0;
        document.dispatchEvent(new Event('learning-chat-read'));
    }
    function schedule() {
        clearTimeout(timer);
        if (!document.hidden) timer = setTimeout(poll, delay);
    }
    async function poll(previous = false) {
        if (fetching || document.hidden) return;
        fetching = true;
        older.disabled = true;
        try {
            const query = previous ? '?before=' + earliest : latest ? '?after=' + latest : '';
            const response = await fetch(endpoint + query, { headers: { Accept: 'application/json' } });
            if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error();
            const data = await response.json();
            const rows = data.messages;
            const firstPage = !latest;
            render(rows, previous);
            if (rows.length) {
                if (firstPage || previous) earliest = rows[0].id;
                if (!previous) {
                    latest = rows.at(-1).id;
                    const last = rows.at(-1);
                    selected.querySelector('.conversation-preview').textContent = last.body;
                }
            }
            if (firstPage || previous) older.hidden = !data.more;
            canSend = data.canSend;
            updateComposer();
            show(data.archived ? 'Archived conversation · Read only' :
                !canSend ? 'Messaging is currently unavailable for this relationship.' :
                rendered.size === 0 ? 'No messages yet. Send the first message.' : '');
            await markDisplayed();
            delay = !previous && !firstPage && data.more ? 100 : 5000;
        } catch {
            show('Could not update messages. The conversation will retry automatically.');
            delay = Math.min(30000, Math.max(delay, 5000) * 2);
        } finally {
            fetching = false;
            older.disabled = false;
            schedule();
        }
    }
    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (sending || !canSend || !input.value.trim()) return;
        sending = true;
        sendError = '';
        updateComposer();
        const body = input.value;
        try {
            const response = await post(endpoint, new URLSearchParams({ body }));
            if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error();
            if (input.value === body) input.value = '';
        } catch {
            sendError = 'Sending could not be confirmed. Check the conversation before retrying.';
            show('');
        } finally {
            sending = false;
            updateComposer();
            poll();
        }
    });
    older.addEventListener('click', () => poll(true));
    document.addEventListener('visibilitychange', () => document.hidden ? schedule() : poll());
    poll();
})();
