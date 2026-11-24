document.addEventListener('DOMContentLoaded', () => {
    const search = document.getElementById('skillsSearchInput');
    const rows = [...document.querySelectorAll('#skillsTableBody tr')];
    const empty = document.getElementById('skillsEmptyState');
    const table = document.getElementById('skillsTable');
    const showing = document.getElementById('skillsShowingText');
    const filters = [...document.querySelectorAll('.admin-filter-btn')];
    const previous = document.getElementById('prevSkillsPage');
    const next = document.getElementById('nextSkillsPage');
    const pageInfo = document.getElementById('skillsPageInfo');
    const pageSize = 5;
    let activeFilter = 'all';
    let currentPage = 1;

    function filteredRows() {
        const query = (search?.value || '').trim().toLowerCase();
        return rows.filter(row => {
            const matchesSearch = !query || row.textContent.toLowerCase().includes(query);
            const matchesFilter = activeFilter === 'all' ||
                (activeFilter === 'active' && row.dataset.status === 'published') ||
                (activeFilter === 'archived' && row.dataset.status === 'archived') ||
                row.dataset.level === activeFilter;
            return matchesSearch && matchesFilter;
        });
    }

    function applyFilters() {
        const matches = filteredRows();
        const totalPages = Math.max(1, Math.ceil(matches.length / pageSize));
        currentPage = Math.min(currentPage, totalPages);
        const start = (currentPage - 1) * pageSize;
        const pageRows = new Set(matches.slice(start, start + pageSize));
        rows.forEach(row => row.hidden = !pageRows.has(row));

        table.hidden = matches.length === 0;
        empty.hidden = matches.length !== 0;
        empty.classList.toggle('hidden', matches.length !== 0);
        const from = matches.length ? start + 1 : 0;
        const to = Math.min(start + pageSize, matches.length);
        showing.textContent = `Showing ${from}–${to} of ${matches.length} skills`;
        pageInfo.textContent = `Page ${currentPage} of ${totalPages}`;
        previous.disabled = currentPage <= 1;
        next.disabled = currentPage >= totalPages;
    }

    search?.addEventListener('input', () => { currentPage = 1; applyFilters(); });
    filters.forEach(button => button.addEventListener('click', () => {
        filters.forEach(item => item.classList.remove('active'));
        button.classList.add('active');
        activeFilter = button.dataset.filter;
        currentPage = 1;
        applyFilters();
    }));

    previous?.addEventListener('click', () => { if (currentPage > 1) { currentPage--; applyFilters(); } });
    next?.addEventListener('click', () => { const pages = Math.max(1, Math.ceil(filteredRows().length / pageSize)); if (currentPage < pages) { currentPage++; applyFilters(); } });

    document.querySelectorAll('[data-view-skill]').forEach(button => {
        button.addEventListener('click', () => {
            let dialog = document.getElementById('skillViewDialog');
            if (!dialog) {
                dialog = document.createElement('dialog');
                dialog.id = 'skillViewDialog';
                dialog.className = 'skill-view-dialog';
                document.body.append(dialog);
            }

            const values = [
                ['Description', button.dataset.description],
                ['Category', button.dataset.category],
                ['Subcategory', button.dataset.subcategory],
                ['Level', button.dataset.level],
                ['Access tier', button.dataset.access],
                ['Publication status', button.dataset.status],
                ['Members using this skill', button.dataset.users]
            ];

            dialog.replaceChildren();
            const header = document.createElement('div');
            header.className = 'skill-view-head';
            const title = document.createElement('h2');
            title.textContent = button.dataset.name;
            const close = document.createElement('button');
            close.type = 'button'; close.className = 'skill-view-close'; close.textContent = '×';
            close.setAttribute('aria-label', 'Close'); close.addEventListener('click', () => dialog.close());
            header.append(title, close);
            const list = document.createElement('dl');
            values.forEach(([label, value]) => {
                const term = document.createElement('dt'); term.textContent = label;
                const detail = document.createElement('dd'); detail.textContent = value || '—';
                list.append(term, detail);
            });
            dialog.append(header, list);
            dialog.showModal();
        });
    });

    document.getElementById('exportSkillsBtn')?.addEventListener('click', () => {
        const visibleRows = filteredRows();
        const csvRows = [['Skill', 'Category', 'Level', 'State', 'Users']];
        visibleRows.forEach(row => {
            const cells = [...row.querySelectorAll('td')];
            csvRows.push([cells[0]?.innerText, cells[1]?.innerText, cells[2]?.innerText, cells[3]?.innerText, cells[4]?.innerText]);
        });
        const escape = value => `"${String(value || '').replaceAll('"', '""')}"`;
        const blob = new Blob(['\uFEFF' + csvRows.map(row => row.map(escape).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = 'skills.csv';
        link.click();
        URL.revokeObjectURL(link.href);
    });

    applyFilters();
});
