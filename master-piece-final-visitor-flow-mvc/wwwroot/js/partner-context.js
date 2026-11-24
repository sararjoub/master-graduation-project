/* Fill the original partner/profile markup from read-only sample catalogue IDs. */
document.addEventListener('DOMContentLoaded',function(){
 'use strict';
 const c=window.SkillSwapCatalogue,q=new URLSearchParams(location.search);
 const p=c.partners.find(p=>p.id===q.get('partner'))||c.partners[0];
 const selected=p.teaches.includes(q.get('skill'))?q.get('skill'):p.teaches[0];
 const name=id=>c.skills.find(s=>s.id===id)?.name||id;
 document.querySelectorAll('.profile-name,.summary-name').forEach(n=>n.textContent=p.name);
 document.querySelectorAll('.profile-avatar span,.summary-avatar span').forEach(n=>n.textContent=p.initials);
 const profileBlocks=document.querySelectorAll('.profile-main .profile-block');
 if(profileBlocks.length){
  const bio=profileBlocks[0].querySelector('p');if(bio)bio.textContent=p.mode==='volunteer'?'I offer voluntary teaching. You do not need to teach a skill in return.':'I enjoy helping people practise skills through clear, agreed learning goals.';
  [p.teaches,p.learns].forEach((ids,i)=>{const target=profileBlocks[i+1]?.querySelector('.skill-tags');if(target){target.replaceChildren();ids.forEach(id=>{const n=document.createElement('span');n.className='skill-tag'+(i?' skill-tag-want':'');n.textContent=name(id);target.append(n);});if(!ids.length){const n=document.createElement('p');n.textContent='No exchange skill required.';target.append(n);}}});
 }
 const send=document.getElementById('sendRequestBtn');
 send?.addEventListener('click',e=>{e.preventDefault();e.stopImmediatePropagation();window.SitePrefs.navigate('exchange-request.html?'+new URLSearchParams({partner:p.id,skill:selected}).toString());},true);
 if(document.getElementById('exchangeForm')){
  const form=document.getElementById('exchangeForm');
  const block=document.createElement('div');block.className='form-block';
  const label=document.createElement('label');label.className='form-block-label';label.htmlFor='learningArrangement';label.textContent='Learning arrangement';
  const select=document.createElement('select');select.id='learningArrangement';select.className='form-input';select.name='arrangement';
  const values=p.mode==='exchange'?[['exchange','Reciprocal skill exchange']]:p.mode==='volunteer'?[['volunteer','Volunteer learning — no exchange needed']]:[['volunteer','Volunteer learning'],['exchange','Reciprocal skill exchange']];
  values.forEach(([v,text])=>{const option=document.createElement('option');option.value=v;option.textContent=text;select.append(option);});
  const hint=document.createElement('p');hint.textContent='Frontend preview only. No request is sent or stored.';hint.style.fontSize='13px';hint.style.color='var(--text-light)';block.append(label,select,hint);form.prepend(block);
  const teach=document.querySelector('[data-name="teach"]')?.closest('.form-block');
  function sync(){if(teach){teach.hidden=select.value==='volunteer';teach.style.display=select.value==='volunteer'?'none':'';}}
  select.addEventListener('change',sync);sync();
  const learn=document.querySelector('[data-name="learn"]');if(learn){learn.replaceChildren();p.teaches.forEach(id=>{const button=document.createElement('button');button.type='button';button.className='chip'+(id===selected?' selected':'');button.dataset.value=name(id);button.textContent=name(id);button.addEventListener('click',()=>{learn.querySelectorAll('.chip').forEach(n=>n.classList.remove('selected'));button.classList.add('selected');});learn.append(button);});}
  document.querySelectorAll('.form-block-label').forEach(l=>{if(l.textContent.includes('teach Ahmad'))l.textContent='What can you teach '+p.name.split(' ')[0]+'?';});
  const message=document.getElementById('message');if(message){message.value='';message.placeholder='Describe your goal and what you would like to practise with '+p.name.split(' ')[0]+'.';}
  form.addEventListener('submit',e=>{
   e.preventDefault();e.stopImmediatePropagation();const text=message.value.trim(),learnChip=learn?.querySelector('.selected'),offer=document.querySelector('[data-name="teach"] .selected'),time=document.querySelector('[data-name="time"] .selected');
   let error='';if(!learnChip)error='Choose the skill you want to learn.';else if(text.length<20)error='Write at least 20 characters about your learning goal.';else if(!time)error='Choose a preferred time.';else if(select.value==='exchange'&&(!offer||!p.learns.some(id=>name(id)===offer.dataset.value)))error='Choose a skill this partner wants to learn, or select a volunteer teacher.';
   if(!error){const params=new URLSearchParams({partner:p.id,skill:selected});window.SitePrefs.navigate('request-sent.html?'+params.toString());return;}
   let output=document.getElementById('requestPreviewFeedback');if(!output){output=document.createElement('p');output.id='requestPreviewFeedback';output.setAttribute('role','alert');form.append(output);}output.textContent=window.SitePrefs?.text?.(error)||error;output.style.color='var(--danger)';output.scrollIntoView({block:'nearest'});
  },true);
 }
});
