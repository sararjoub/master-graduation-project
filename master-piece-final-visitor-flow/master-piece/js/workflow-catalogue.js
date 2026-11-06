/* Read-only examples; replace with server-provided view models. No persistence. */
window.SkillSwapCatalogue = Object.freeze({
 skills: [
  {id:'web-development',name:'Web Development'}, {id:'html',name:'HTML Fundamentals'}, {id:'javascript',name:'JavaScript'}, {id:'react',name:'React'},
  {id:'ui-ux-design',name:'UI/UX Design'}, {id:'graphic-design',name:'Graphic Design'}, {id:'photography',name:'Photography'}, {id:'python',name:'Python'}, {id:'figma',name:'Figma Basics'}, {id:'user-research',name:'User Research'}, {id:'advanced-python',name:'Advanced Python'}, {id:'machine-learning',name:'Machine Learning'}, {id:'react-native',name:'React Native'}, {id:'devops',name:'DevOps'}
 ],
 partners: [
  {id:'ahmad-hassan',name:'Ahmad Hassan',initials:'AH',mode:'exchange',teaches:['web-development','javascript','react','react-native'],learns:['ui-ux-design','graphic-design'],expert:['web-development','javascript']},
  {id:'lana-khalid',name:'Lana Khalid',initials:'LK',mode:'both',teaches:['ui-ux-design','graphic-design','figma','user-research'],learns:['web-development'],expert:['ui-ux-design','user-research']},
  {id:'sara-mahmoud',name:'Sara Mahmoud',initials:'SM',mode:'volunteer',teaches:['web-development','html','photography'],learns:[],expert:[]},
  {id:'omar-sami',name:'Omar Sami',initials:'OS',mode:'both',teaches:['javascript','react','python','advanced-python','machine-learning','devops'],learns:['graphic-design'],expert:['javascript']}
 ]
});
