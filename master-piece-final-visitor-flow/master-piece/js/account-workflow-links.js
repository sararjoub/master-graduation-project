document.addEventListener('DOMContentLoaded',function(){
 'use strict';
 const menu=document.getElementById('userDropdown');if(!menu)return;
 [['apply-expert.html','Apply as Expert'],['expert-workspace.html','Expert Workspace'],['subscription.html','My Subscription'],['sessions.html','My Sessions']].forEach(([href,label])=>{if(menu.querySelector('a[href="'+href+'"]'))return;const a=document.createElement('a');a.className='dropdown-item';a.href=href;a.textContent=label;menu.append(a);});
});
