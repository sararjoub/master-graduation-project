/* Extend the original popup without replacing the skill-page design. */
document.addEventListener('DOMContentLoaded',function(){
 'use strict';
 const catalogue=window.SkillSwapCatalogue,params=new URLSearchParams(location.search);
 const aliases={'web-dev':'web-development','ui-ux':'ui-ux-design','react-development':'react','html-css':'html'};
 const raw=params.get('skill')||'web-development',id=aliases[raw]||raw;
 const skill=catalogue.skills.find(s=>s.id===id);
 if(skill){const title=document.querySelector('h1.skill-title');if(title)title.textContent=skill.name;document.title=skill.name+' - Skills Swap';}
 const start=document.getElementById('startLearningBtn'),modal=document.getElementById('learningOptionsModal');
 if(!start||!modal)return;
 function render(kind){
  const body=modal.querySelector('.learning-modal-body');body.replaceChildren();
  const top=document.createElement('div');top.className='form-actions';
  ['community','expert'].forEach(type=>{const b=document.createElement('button');b.type='button';b.setAttribute('aria-pressed',String(type===kind));b.className='btn '+(type===kind?'btn-primary':'btn-outline');b.textContent=type==='community'?'Community partners':'Experts · Plus';b.addEventListener('click',()=>render(type));top.append(b);});body.append(top);
  const people=catalogue.partners.filter(p=>(kind==='expert'?p.expert:p.teaches).includes(id));
  if(!people.length){const p=document.createElement('p');p.textContent='No matching '+(kind==='expert'?'experts':'community partners')+' in the sample catalogue. Try another skill or contact support.';body.append(p);}
  people.forEach(p=>{const a=document.createElement('a');a.className='learning-option-card';a.href=(kind==='expert'?'expert-booking.html':'partner-profile.html')+'?'+new URLSearchParams({partner:p.id,skill:id}).toString();
   const icon=document.createElement('span');icon.className='option-icon';icon.textContent=p.initials;
   const text=document.createElement('div');text.className='option-text';const h=document.createElement('h3');h.textContent=p.name;const info=document.createElement('p');info.textContent=kind==='expert'?'Approved for this skill · Plus + session credit required':p.mode==='exchange'?'Reciprocal exchange — offer a compatible skill':p.mode==='volunteer'?'Volunteer teacher — no exchange skill needed':'Exchange or volunteer learning';text.append(h,info);a.append(icon,text);body.append(a);
  });
  const a=document.createElement('a');a.href='AI.html?skill='+encodeURIComponent(id);a.className='btn btn-outline';a.textContent='Get AI recommendations and guidance';body.append(a);
 }
 start.addEventListener('click',e=>{e.preventDefault();e.stopImmediatePropagation();render('community');modal.classList.remove('hidden');modal.setAttribute('role','dialog');modal.setAttribute('aria-modal','true');document.body.style.overflow='hidden';modal.querySelector('button')?.focus();},true);
 const close=document.getElementById('closeLearningModal');close?.setAttribute('aria-label','Close learning options');
 const originalClose=()=>{modal.classList.add('hidden');document.body.style.overflow='';start.focus();};
 modal.addEventListener('keydown',e=>{if(e.key==='Escape'){e.preventDefault();originalClose();}if(e.key==='Tab'){const nodes=[...modal.querySelectorAll('a[href],button:not(:disabled)')].filter(n=>n.offsetParent!==null),first=nodes[0],last=nodes[nodes.length-1];if(e.shiftKey&&document.activeElement===first){e.preventDefault();last?.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first?.focus();}}});
});


document.addEventListener('DOMContentLoaded',function(){const q=new URLSearchParams(location.search),id=window.SkillAccess.id(q.get('skill')||'web-development'),skill=window.SkillSwapCatalogue.skills.find(s=>s.id===id);if(!skill)return;const text=s=>window.SitePrefs?.text?.(s)||s;const advanced=window.SkillAccess.requiresPlus(id);document.querySelector('.breadcrumb .current').textContent=skill.name;const badge=document.querySelector('.skill-header .badge-success');if(badge)badge.textContent=text(advanced?'Advanced · Plus':'Basic · Free');const level=document.querySelector('.skill-header .badge-info');if(level)level.textContent=text(advanced?'Advanced':'Beginner');if(id!=='web-development'){const intro=document.querySelector('.skill-intro');intro.textContent=text('Learn this skill through practical sessions with a community partner or an approved expert.');const about=document.querySelector('.skill-section p');if(about)about.textContent=text('Agree on your starting level, one clear goal, and a suitable time with your learning partner.');const grid=document.querySelector('.learn-grid');if(grid){grid.replaceChildren();['Understand the foundations','Practise with a learning partner','Apply the skill to a small project','Review your progress together'].forEach(label=>{const item=document.createElement('div');item.className='learn-item';const check=document.createElement('span');check.className='learn-check';check.textContent='✓';const caption=document.createElement('span');caption.textContent=text(label);item.append(check,caption);grid.append(item);});}}});

document.addEventListener('DOMContentLoaded',function(){if(new URLSearchParams(location.search).get('support')==='expert'&&window.SkillAccess.isPlus()){document.getElementById('startLearningBtn')?.click();document.querySelectorAll('#learningOptionsModal .form-actions button')[1]?.click();}});
