/* Sequential timeline demo. Production progress and membership come from server records. */
document.addEventListener('DOMContentLoaded',function(){
 'use strict';const q=new URLSearchParams(location.search),text=s=>window.SitePrefs?.text?.(s)||s;
 const goals={
  'full-stack':{title:'Full Stack Web Developer',skills:[['html','HTML Fundamentals'],['css','CSS & Responsive Design'],['javascript','JavaScript Basics'],['git-github','Git & GitHub'],['react','Frontend Framework (React)'],['backend-development','Backend Development'],['database-apis','Database & APIs'],['full-stack-project','Full Stack Project']]},
  'ui-ux':{title:'UI/UX Designer',skills:[['ui-ux-design','UI/UX Design'],['graphic-design','Graphic Design'],['user-research','User Research']]},
  data:{title:'Data Scientist',skills:[['python','Python'],['advanced-python','Advanced Python'],['machine-learning','Machine Learning']]},
  graphic:{title:'Graphic Designer',skills:[['graphic-design','Graphic Design'],['ui-ux-design','UI/UX Design'],['user-research','User Research']]},
  mobile:{title:'Mobile App Developer',skills:[['web-development','Web Development'],['javascript','JavaScript'],['react-native','React Native']]},
  devops:{title:'DevOps Engineer',skills:[['python','Python'],['advanced-python','Advanced Python'],['devops','DevOps']]}
 };
 const chosen=goals[q.get('goal')],context=new URLSearchParams();['goal','level','time','known'].forEach(key=>{const value=q.get(key);if(value)context.set(key,value);});
 let done=chosen?Math.min(chosen.skills.length,Math.max(0,Number.parseInt(q.get('completed'),10)||0)):0;
 if(chosen)context.set('completed',String(done));
 function route(path){return window.SitePrefs.decorate(path+(context.size?'?'+context.toString():''));}
 const confirm=document.getElementById('confirmCareerPath');if(confirm){context.set('completed','0');confirm.href=route('career-path.html');context.set('completed',String(done));}const refine=document.getElementById('refineCareerPath');if(refine)refine.href=route('career-path-setup.html');
 function a(label,href){const link=document.createElement('a');link.className='btn btn-primary';link.textContent=text(label);link.href=href;return link;}
 function updateLinks(){if(chosen)document.querySelectorAll('a[href]').forEach(link=>{const url=new URL(link.href,location.href);if(/\/(dashboard|career-path|AI)\.html$/.test(url.pathname)&&!url.pathname.includes('/admin-html/')){context.forEach((v,k)=>url.searchParams.set(k,v));link.href=window.SitePrefs.decorate(url.href);}});}
 function summary(host){const h=document.createElement('h2'),p=document.createElement('p');h.textContent=text(chosen?'Your career path':'No career path selected');p.textContent=text(chosen?'Your suggested path is ready.':'Explore skills directly, or create a path whenever you are ready.');host.append(h,p);if(!chosen){host.append(a('Create my career path',route('career-path-setup.html')));return;}const title=document.createElement('h3');title.textContent=text(chosen.title);const count=document.createElement('p');count.textContent=text('Completed steps')+': '+done+' / '+chosen.skills.length;const progress=document.createElement('progress');progress.max=chosen.skills.length;progress.value=done;progress.setAttribute('aria-label',text('Completed steps'));host.append(title,count,progress,a('View My Career Path',route('career-path.html')));}
 const legacyRoot=document.querySelector('.career-section');if(legacyRoot)window.renderOriginalCareerPath?.(chosen||goals['full-stack'],done,context);
 if(location.pathname.endsWith('/dashboard.html')&&!location.pathname.includes('/admin-html/')){const card=document.createElement('section');card.className='career-dashboard-card';summary(card);document.querySelector('.dashboard-section .welcome-header')?.after(card);}
 updateLinks();
});


