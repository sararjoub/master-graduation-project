document.addEventListener('DOMContentLoaded', () => {

    // 1. قراءة الفئة من الرابط (URL)
    const urlParams = new URLSearchParams(window.location.search);
    const categoryKey = urlParams.get('cat') || 'web-development';

    // 2. قاعدة بيانات وهمية (مؤقتة) للمهارات
    // لاحقاً في الباك إند، هذه البيانات ستأتي من قاعدة البيانات
    const skillsDatabase = {
        'web-development': {
            title: 'Web Development', icon: '💻',
            skills: [
                { name: 'HTML & CSS', level: 'Beginner', price: 'Free', desc: 'Build the structure and style of websites.' },
                { name: 'JavaScript', level: 'Intermediate', price: 'Free', desc: 'Add interactivity and logic to your pages.' },
                { name: 'React.js', level: 'Advanced', price: 'Premium', desc: 'Build modern, component-based user interfaces.' }
            ]
        },
        'ui-ux': {
            title: 'UI/UX Design', icon: '🎨',
            skills: [
                { name: 'Figma Basics', level: 'Beginner', price: 'Free', desc: 'Learn to design user interfaces.' },
                { name: 'User Research', level: 'Intermediate', price: 'Premium', desc: 'Understand user needs and behaviors.' }
            ]
        },
        // يمكنك إضافة باقي الفئات هنا بنفس الطريقة...
    };

    Object.assign(skillsDatabase, {"mathematics": {"title": "Mathematics", "icon": "📚", "skills": [{"name": "Mathematics Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "physics": {"title": "Physics", "icon": "📚", "skills": [{"name": "Physics Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "chemistry": {"title": "Chemistry", "icon": "📚", "skills": [{"name": "Chemistry Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "english": {"title": "English", "icon": "📚", "skills": [{"name": "English Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "arabic": {"title": "Arabic Language", "icon": "📚", "skills": [{"name": "Arabic Language Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "mobile-development": {"title": "Mobile Development", "icon": "📚", "skills": [{"name": "Mobile Development Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "cybersecurity": {"title": "Cybersecurity", "icon": "📚", "skills": [{"name": "Cybersecurity Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "painting": {"title": "Painting", "icon": "📚", "skills": [{"name": "Painting Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "knitting": {"title": "Knitting", "icon": "📚", "skills": [{"name": "Knitting Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "crafts": {"title": "Crafts", "icon": "📚", "skills": [{"name": "Crafts Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "diy": {"title": "DIY Projects", "icon": "📚", "skills": [{"name": "DIY Projects Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "music": {"title": "Music", "icon": "📚", "skills": [{"name": "Music Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "video-editing": {"title": "Video Editing", "icon": "📚", "skills": [{"name": "Video Editing Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "photography": {"title": "Photography", "icon": "📚", "skills": [{"name": "Photography Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "marketing": {"title": "Marketing", "icon": "📚", "skills": [{"name": "Marketing Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "management": {"title": "Management", "icon": "📚", "skills": [{"name": "Management Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "sales": {"title": "Sales", "icon": "📚", "skills": [{"name": "Sales Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "entrepreneurship": {"title": "Entrepreneurship", "icon": "📚", "skills": [{"name": "Entrepreneurship Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "yoga": {"title": "Yoga", "icon": "📚", "skills": [{"name": "Yoga Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "nutrition": {"title": "Nutrition", "icon": "📚", "skills": [{"name": "Nutrition Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "personal-training": {"title": "Personal Training", "icon": "📚", "skills": [{"name": "Personal Training Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "performance": {"title": "Performance", "icon": "📚", "skills": [{"name": "Performance Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "fitness": {"title": "Fitness", "icon": "📚", "skills": [{"name": "Fitness Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}, "wellness": {"title": "Wellness", "icon": "📚", "skills": [{"name": "Wellness Fundamentals", "level": "Beginner", "price": "Free", "desc": "Introductory practice example. Full learning resources will be reviewed before publication."}]}});

    // 3. تحديث واجهة الصفحة بناءً على الفئة
    const categoryData = skillsDatabase[categoryKey] || {title: 'Subcategory not found', icon: '📚', skills: []};
    
    document.getElementById('subCategoryTitle').textContent = categoryData.title;
    document.getElementById('subCategoryIcon').textContent = categoryData.icon;
    document.getElementById('breadcrumbCategory').textContent = categoryData.title;

    // 4. عرض المهارات
    const skillsGrid = document.getElementById('skillsGrid');
    skillsGrid.innerHTML = ''; // مسح المحتوى القديم

    categoryData.skills.forEach(skill => {
        const badgeClass = skill.price === 'Premium' ? 'badge-warning' : 'badge-success';
        
        const card = document.createElement('div');
        card.className = 'skill-card';
        card.dataset.access=skill.price==='Premium'?'plus':'basic';
        card.dataset.skillId=window.SkillAccess.id(skill.name);
        card.innerHTML = `
            <div class="skill-badges">
                <span class="badge badge-info">${skill.level}</span>
                <span class="badge ${badgeClass}">${skill.price === 'Premium' ? 'Advanced · Plus' : 'Basic · Free'}</span>
            </div>
            <h3 class="skill-title">${skill.name}</h3>
            <p class="skill-desc">${skill.desc}</p>
            <button class="start-learning-btn" onclick="showLoginRequired()">Start Learning</button>
        `;
        skillsGrid.appendChild(card);
    });

    // 5. منطق ظهور نافذة تسجيل الدخول
    window.showLoginRequired = () => {
        document.getElementById('loginRequiredModal').classList.remove('hidden');
    };

    document.getElementById('closeLoginModal').addEventListener('click', () => {
        document.getElementById('loginRequiredModal').classList.add('hidden');
    });
});
document.addEventListener('DOMContentLoaded',function(){document.querySelectorAll('#skillsGrid .skill-card').forEach(card=>{const b=card.querySelector('.start-learning-btn');if(!b)return;b.classList.add('btn','btn-primary');b.removeAttribute('onclick');b.addEventListener('click',()=>{if(card.dataset.access==='plus'&&!window.SkillAccess.isPlus()){window.SkillAccess.showUpgrade();return;}window.SitePrefs.navigate('skill-details.html?skill='+encodeURIComponent(card.dataset.skillId));});});});
