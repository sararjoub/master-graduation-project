document.addEventListener('DOMContentLoaded',function(){
 'use strict';const dialog=document.getElementById('adminProfileEditor'),form=document.getElementById('adminClassicProfileForm'),button=document.getElementById('editAdminProfile');
 button.addEventListener('click',()=>{form.elements.name.value=document.getElementById('adminProfileName').textContent;form.elements.country.value=document.getElementById('adminProfileCountry').textContent;form.elements.about.value=document.getElementById('adminProfileAbout').textContent;dialog.showModal();});
 document.getElementById('cancelAdminProfile').addEventListener('click',()=>dialog.close());
 form.addEventListener('submit',e=>{e.preventDefault();if(!form.reportValidity())return;[['adminProfileName','name'],['adminProfileDisplay','name'],['adminProfileCountry','country'],['adminProfileAbout','about']].forEach(([id,key])=>{const element=document.getElementById(id);element.dataset.userContent='';element.textContent=form.elements[key].value.trim();});dialog.close();});
 dialog.addEventListener('close',()=>button.focus());
});
