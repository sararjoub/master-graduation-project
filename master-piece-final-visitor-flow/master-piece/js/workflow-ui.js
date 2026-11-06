/* UI validation and animation only. No browser persistence and no server claims. */
(function () {
    'use strict';
    const safe = value => String(value || '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    function feedback(form, text, error) {
        const output = form.querySelector(error ? '.wf-errors' : '.wf-feedback');
        const other = form.querySelector(error ? '.wf-feedback' : '.wf-errors');
        if (other) other.textContent = '';
        if (output) { output.textContent = text; output.scrollIntoView({block:'nearest', behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth'}); }
    }
    function dialog(title, text) {
        let d = document.getElementById('wf-dialog');
        if (!d) { d=document.createElement('dialog');d.id='wf-dialog';d.className='wf-dialog';d.setAttribute('aria-labelledby','wf-dialog-title');document.body.append(d); }
        d.replaceChildren();
        const header=document.createElement('div');header.className='wf-dialog-head';
        const heading=document.createElement('h2');heading.id='wf-dialog-title';heading.textContent=title;
        const close=document.createElement('button');close.className='wf-close';close.type='button';close.textContent='×';close.setAttribute('aria-label','Close dialog');close.addEventListener('click',()=>d.close());
        const p=document.createElement('p');p.className='wf-rich';p.textContent=text;header.append(heading,close);d.append(header,p);d.showModal();
    }
    window.WF = {
        dialog, safe,
        bindBooking() {
            const q=new URLSearchParams(location.search), data=window.SkillSwapCatalogue;
            const p=data?.partners.find(p=>p.id===q.get('partner')), s=data?.skills.find(s=>s.id===q.get('skill'));
            if(p)document.getElementById('wf-expert-name').textContent=p.name;
            if(s)document.getElementById('wf-booking-skill').textContent=s.name;
        }
    };
    document.addEventListener('click',e=>{
        const button=e.target.closest('[data-wf-action]');if(!button)return;
        if(button.dataset.wfAction==='resend')dialog('Confirmation email preview','If an eligible account exists, a new secure confirmation link would be requested. No email was sent by this frontend.');
    });
    document.addEventListener('submit',e=>{
        const f=e.target.closest('form[data-wf-form]');if(!f)return;e.preventDefault();if(!f.reportValidity())return;
        const data=new FormData(f),get=name=>String(data.get(name)||'').trim(),kind=f.dataset.wfForm;
        try {
            if(['reset','admin-password'].includes(kind)){
                if(get('password')!==get('confirm'))throw Error('The new password and confirmation do not match.');
                feedback(f,'Validation passed. No password was stored or changed.');f.reset();return;
            }
            if(kind==='recovery'){feedback(f,'If a matching account exists, the connected service would send a recovery link. No email was sent.');return;}
            if(kind==='plan-request'){window.SitePrefs.navigate('AI.html');return;}
            if(kind==='expert-booking')throw Error('This sample learner has Free access and no expert credits. Review Plus first. The backend will verify entitlement, credit, expert approval, and slot availability.');
            if(kind==='checkout'){feedback(f,'Checkout summary: Plus · '+(get('billing')==='yearly'?'$84 per year':'$9 per month')+' (proposed). Includes 1 expert credit per subscription month. No payment was made and no subscription was activated.');return;}
            if(kind==='expert-application'&&get('route')==='community')throw Error('A real community application requires verified teaching history. This sample has no verified history; use the external-evidence route for a preview.');
            if(kind==='admin-skill-peer'){const allowed={technology:['web','design'],education:['languages'],handmade:['art']};if(!allowed[get('category')]?.includes(get('subcategory')))throw Error('Choose a subcategory that belongs to the selected main category.');}
            if(kind==='admin-sequence'){const allowed=['web-development','javascript','react','ui-ux-design','graphic-design','python','photography','html'];const ids=get('skills').split('\n').map(x=>x.trim()).filter(Boolean);if(!ids.length||ids.some(x=>!allowed.includes(x))||new Set(ids).size!==ids.length)throw Error('Use valid, unique published skill IDs in the intended order.');}
            if(kind==='availability'&&get('start')>=get('end'))throw Error('Choose an end time after the start time.');
            if(kind==='session-change'&&get('operation')==='reschedule'&&!get('newdate'))throw Error('Choose a proposed date and time. The original appointment remains until the partner accepts.');
            if(kind==='admin-pricing'&&Number(get('annual'))<Number(get('monthly')))throw Error('Check the annual amount; it is lower than one month of the proposed price.');
            const messages={
                'account-report':'Preview: '+get('decision')+'. Reason: '+get('reason')+'. No private conversation or meeting was accessed; no automatic report threshold is applied.',
                'admin-sequence':'Sequence preview validated. Publishing the curated order requires the backend. No personal learner plan was changed.',
                'admin-skill-peer':'Skill details validated for preview. All members can use community learning; no paid tier is assigned to the skill.',
                
                'expert-application':'Preview: application prepared for human review. No expert badge was granted or saved.',
                'availability':'Preview: availability validated. No calendar slots were saved.',
                'cancel-renewal':'Preview: cancellation would stop future renewal; paid-through access and confirmed eligible bookings would remain. No subscription was changed.',
                'session-change':'Preview: '+(get('operation')==='reschedule'?'reschedule proposal requires partner acceptance.':get('operation')==='cancel'?'cancellation eligibility must be checked before returning a credit once.':'the attendance concern needs support review.')+' No appointment was changed.',
                'experience-review':'Preview: review validated. The server must verify completed participation and enforce one review per experience before publishing.',

                'report':'Preview: concern prepared for investigation. No report was sent or saved.',
                'support':'Preview: support ticket validated. No ticket or external email was sent.',
                'admin-plan':'Preview decision: '+get('decision')+'. Reason: '+get('reason')+'. A real decision requires server authorization and an audit entry.',
                'admin-evidence':'Preview evidence decision: '+get('decision')+'. Feedback: '+get('reason')+'. No learner progress was changed.',
                'admin-expert':'Preview skill-specific decision: '+get('decision')+'. Feedback: '+get('reason')+'. No actual approval was granted.',
                'admin-session':'Preview outcome: '+get('decision')+'. A server credit ledger must prevent duplicate returns. No session or money was changed.',
                'admin-moderation':'Preview: '+get('decision')+'. Investigation reason: '+get('reason')+'. No account or content was restricted.',
                'admin-support':'Preview reply: '+get('reply')+'. Status: '+get('status')+'. No message was sent externally.',
                'admin-pricing':'Proposed future pricing validated. Existing subscription terms are unchanged; nothing was saved.',
                'admin-profile':'Display-name preview validated. No staff account was modified.',
                'admin-content':'Draft content validated for review. It was not published or saved.',
                'admin-edit-user':'Preview: '+get('name')+' / '+get('state')+'. Reason: '+get('reason')+'. No account access was changed.',
                'admin-template':'Template preview validated. Existing approved learner plans keep their own version. Nothing was saved.',
                'admin-course':'Resource details validated. Publication and resource ownership require server review; nothing was saved.'
            };
            feedback(f,messages[kind]||'Preview validation completed. No data was saved.');
            if(kind==='admin-plan'){const badge=document.getElementById('wf-plan-decision');if(badge)badge.textContent=get('decision')+' · page preview only';}
        } catch(error){feedback(f,error.message,true);}
    });
})();
