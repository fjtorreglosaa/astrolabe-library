
const TINTS = ['#0E5A6E','#0B2E3B','#12766B','#1F5F8B','#0F8A7A','#2A6E7E','#164A5C','#3A4E7A'];

const DEMO_EMAIL = 'fjtorreglosaa@gmail.com';
const DEMO_PASS = 'Testing1234*';
const DEMO_ACCOUNTS = {
  'fjtorreglosaa@gmail.com': {role:'Plus', name:'Francisco Torreglosa', hello:'Welcome back, Francisco. You have 1 overdue book.'},
  'admin@astrolabe.co': {role:'Admin', name:'Dana Whitfield', hello:'Signed in as Admin — Midtown and Harlem.'},
  'super@astrolabe.co': {role:'Super Admin', name:'Francisco Torreglosa', hello:'Signed in as Super Admin — every library.'}
};

const REPAIR_REASONS = ['Damaged spine','Water damage','Missing pages','Rebinding','Cover replacement','Other'];
const DELETE_REASONS = ['Donated','Damaged beyond repair','Lost by member','Withdrawn from collection','Other'];

const COUNTRIES = [
  {name:'United States', cities:['New York','Chicago','Austin']},
  {name:'Canada', cities:['Toronto','Vancouver','Montreal']},
  {name:'United Kingdom', cities:['London','Manchester','Edinburgh']},
  {name:'Mexico', cities:['Mexico City','Guadalajara','Monterrey']},
  {name:'Colombia', cities:['Bogota','Medellin','Cali']},
  {name:'Spain', cities:['Madrid','Barcelona','Valencia']}
];

const BOOKS = [
  {id:1,title:'The House of the Spirits',author:'Isabel Allende',genre:'Fiction',year:1982,pages:448,isbn:'978-0-553-38380-6',copies:'4 / 6',price:'$18.00',rating:'4.7',branch:'New York — Midtown',blurb:'A family saga spanning a century of Chilean history, between the political and the fantastic.'},
  {id:2,title:'Pedro Paramo',author:'Juan Rulfo',genre:'Fiction',year:1955,pages:124,isbn:'978-968-16-7515-3',copies:'2 / 5',price:'$14.00',rating:'4.6',branch:'Chicago — Loop',blurb:'A son searches for his father in a town inhabited by murmurs. Short, dry and decisive.'},
  {id:3,title:'Discipline and Punish',author:'Michel Foucault',genre:'Essay',year:1975,pages:352,isbn:'978-0-679-75255-4',copies:'1 / 3',price:'$22.00',rating:'4.4',branch:'New York — Midtown',blurb:'A history of punishment and the birth of surveillance as a technique of power.'},
  {id:4,title:'Papyrus',author:'Irene Vallejo',genre:'Essay',year:2019,pages:452,isbn:'978-1-4736-9855-5',copies:'6 / 8',price:'$26.00',rating:'4.8',branch:'Austin — Mueller',blurb:'The invention of books in the ancient world, told as a travel chronicle.'},
  {id:5,title:'Klara and the Sun',author:'Kazuo Ishiguro',genre:'Science fiction',year:2021,pages:320,isbn:'978-1-5290-1150-3',copies:'3 / 4',price:'$19.00',rating:'4.3',branch:'New York — Harlem',blurb:'An artificial friend observes the family that bought her. On care, obsolescence and faith.'},
  {id:6,title:'The Savage Detectives',author:'Roberto Bolano',genre:'Fiction',year:1998,pages:609,isbn:'978-0-312-42748-0',copies:'0 / 3',price:'$24.00',rating:'4.5',branch:'Chicago — Loop',blurb:'Two poets search for a vanished poet across twenty years and four continents.'},
  {id:7,title:'Sapiens',author:'Yuval Noah Harari',genre:'History',year:2011,pages:496,isbn:'978-0-06-231609-7',copies:'5 / 7',price:'$23.00',rating:'4.4',branch:'Austin — Mueller',blurb:'A history of humankind from cognition to biotechnology.'},
  {id:8,title:'One Hundred Years of Solitude',author:'Gabriel Garcia Marquez',genre:'Fiction',year:1967,pages:471,isbn:'978-0-06-088328-7',copies:'7 / 10',price:'$20.00',rating:'4.9',branch:'New York — Midtown',blurb:'Macondo, the Buendias and a lineage condemned to repeat itself.'},
  {id:9,title:'Oblivion: A Memoir',author:'Hector Abad Faciolince',genre:'Biography',year:2006,pages:280,isbn:'978-0-231-16947-1',copies:'2 / 4',price:'$16.00',rating:'4.7',branch:'Chicago — Pilsen',blurb:'A portrait of a father murdered in Medellin, written as an act of memory.'},
  {id:10,title:'The Time of the Hero',author:'Mario Vargas Llosa',genre:'Fiction',year:1963,pages:432,isbn:'978-0-374-52748-1',copies:'3 / 5',price:'$17.00',rating:'4.2',branch:'Austin — Mueller',blurb:'Violence as a system inside a Lima military academy.'},
  {id:11,title:'Designing Data-Intensive Applications',author:'Martin Kleppmann',genre:'Technical',year:2017,pages:590,isbn:'978-1-4493-7332-0',copies:'2 / 2',price:'$45.00',rating:'4.8',branch:'New York — Harlem',blurb:'Foundations of distributed data: replication, partitioning and consistency.'},
  {id:12,title:'Kafka on the Shore',author:'Haruki Murakami',genre:'Fiction',year:2002,pages:656,isbn:'978-1-4000-7927-8',copies:'4 / 6',price:'$21.00',rating:'4.3',branch:'New York — Midtown',blurb:'Two stories that cross paths between talking cats and private libraries.'}
];

const DELIVERY_FEE = 3.99;

const PREF_GROUPS = [
  {key:'delivery', label:'Book delivery', note:'How a reserved book reaches you', icon:'local_shipping', opts:[
    {v:'pickup', label:'Pick up at library', note:'Ready in 2 h · free', icon:'store'},
    {v:'home', label:'Home delivery', note:'24–48 h · +$3.99', icon:'local_shipping'}
  ]},
  {key:'ret', label:'Returns', note:'How you give the book back', icon:'assignment_return', opts:[
    {v:'courier', label:'Courier pickup', note:'A courier collects it at your door', icon:'local_shipping'},
    {v:'branch', label:'Drop off at library', note:'Hand it to the desk yourself', icon:'store'}
  ]},
  {key:'purchase', label:'Purchases', note:'How books you buy are fulfilled', icon:'shopping_bag', opts:[
    {v:'pickup', label:'Collect at library', note:'Ready in 2 h · free', icon:'store'},
    {v:'ship', label:'Ship to my address', note:'3–5 days · +$3.99', icon:'local_shipping'}
  ]}
];

const HOME_BRANCH = {'New York':'Midtown', 'Chicago':'Loop', 'Austin':'Mueller'};

const META = {
  1:{tier:'Basic', stock:[{branch:'Midtown',city:'New York',n:2},{branch:'Harlem',city:'New York',n:1},{branch:'Loop',city:'Chicago',n:1}]},
  2:{tier:'Basic', stock:[{branch:'Loop',city:'Chicago',n:2},{branch:'Mueller',city:'Austin',n:1}]},
  3:{tier:'Max', stock:[{branch:'Midtown',city:'New York',n:1}]},
  4:{tier:'Plus', stock:[{branch:'Mueller',city:'Austin',n:3},{branch:'Midtown',city:'New York',n:2},{branch:'Pilsen',city:'Chicago',n:1}]},
  5:{tier:'Plus', stock:[{branch:'Harlem',city:'New York',n:2},{branch:'Midtown',city:'New York',n:1}]},
  6:{tier:'Plus', stock:[{branch:'Loop',city:'Chicago',n:0}]},
  7:{tier:'Basic', stock:[{branch:'Mueller',city:'Austin',n:3},{branch:'Midtown',city:'New York',n:2}]},
  8:{tier:'Basic', stock:[{branch:'Midtown',city:'New York',n:4},{branch:'Loop',city:'Chicago',n:2},{branch:'Mueller',city:'Austin',n:1}]},
  9:{tier:'Plus', stock:[{branch:'Pilsen',city:'Chicago',n:2}]},
  10:{tier:'Plus', stock:[{branch:'Mueller',city:'Austin',n:3}]},
  11:{tier:'Max', stock:[{branch:'Harlem',city:'New York',n:2}]},
  12:{tier:'Plus', stock:[{branch:'Midtown',city:'New York',n:2},{branch:'Loop',city:'Chicago',n:1}]}
};

const TIER_RANK = {Basic:0, Plus:1, Max:2};

const ROLES = [
  {key:'Basic', kind:'member', note:'Member · Basic catalog, home library'},
  {key:'Plus', kind:'member', note:'Member · full catalog in your city'},
  {key:'Max', kind:'member', note:'Member · every library, points'},
  {key:'Admin', kind:'admin', note:'Manages the libraries assigned to them'},
  {key:'Super Admin', kind:'super', note:'Manages every library, assigns admins'}
];

const LIBRARIES = ['New York — Midtown','New York — Harlem','Chicago — Loop','Chicago — Pilsen','Austin — Mueller'];
const ADMIN_SCOPE = ['New York — Midtown','New York — Harlem'];

const BOOK_STATUS = {
  draft:{label:'Draft', bg:'rgba(224,166,60,.20)', fg:'#8A6A28', icon:'edit_note'},
  catalog:{label:'In catalog', bg:'rgba(16,168,140,.14)', fg:'#0C7F70', icon:'menu_book'},
  repair:{label:'In repair', bg:'rgba(31,95,139,.16)', fg:'#1F5F8B', icon:'build'},
  deleted:{label:'Deleted', bg:'rgba(179,38,30,.12)', fg:'#B3261E', icon:'delete'}
};

const WIZ_STEPS = ['Book details', 'Copies & pricing', 'Review'];

const ADMIN_TEAM = [
  {name:'Dana Whitfield', email:'dana@astrolabe.co', role:'Admin', libs:['New York — Midtown','New York — Harlem'], since:'2023'},
  {name:'Marcus Oyelaran', email:'marcus@astrolabe.co', role:'Admin', libs:['Chicago — Loop','Chicago — Pilsen'], since:'2024'},
  {name:'Priya Raman', email:'priya@astrolabe.co', role:'Admin', libs:['Austin — Mueller'], since:'2025'},
  {name:'Francisco Torreglosa', email:'fjtorreglosaa@gmail.com', role:'Super Admin', libs:LIBRARIES, since:'2021'}
];

const MANUALS_SEED = [
  {code:'MP-48210', user:'Nadia Haddad', email:'nadia.h@mail.com', kind:'Fine', concept:'Late fines · 1 title', amount:385, library:'New York — Harlem', created:'Aug 14, 2026 · 10:12', method:'Cash at desk', status:'pending', note:''},
  {code:'MP-48204', user:'Tom\u00e1s Iriarte', email:'t.iriarte@correo.mx', kind:'Subscription', concept:'Subscription — Plus · 1 month', amount:900, library:'Chicago — Pilsen', created:'Aug 14, 2026 · 16:40', method:'Card at desk', status:'pending', note:''},
  {code:'MP-48188', user:'Yusuf Demir', email:'yusuf.demir@mail.com', kind:'Fine', concept:'Late fines · 2 titles', amount:1085, library:'New York — Midtown', created:'Aug 11, 2026 · 09:05', method:'Cash at desk', status:'validated', note:'Validated by Dana Whitfield'},
  {code:'MP-48170', user:'Grace Abbott', email:'grace.abbott@mail.com', kind:'Subscription', concept:'Subscription — Max · 1 month', amount:1600, library:'Austin — Mueller', created:'Aug 6, 2026 · 12:22', method:'Cash at desk', status:'rejected', note:'Member never came to the desk'}
];

const HOME_LIBRARY = 'New York — Midtown';
const TODAY_ISO = '2026-08-15';
const PLAN_CENTS = {Basic:0, Plus:699, Max:1299};
const PLAN_RANK = {Basic:0, Plus:1, Max:2};
const CYCLE = {start:'Aug 12, 2026', renews:'Sep 12, 2026', days:31, left:28};

const EMPTY_MANUAL = {email:'', kind:'Fine', plan:'Plus', amount:'', method:'Cash at desk', note:''};

const FINES_SEED = [
  {id:'f1', title:'The Savage Detectives', reason:'20 days late', cents:700, date:'Aug 12, 2026'},
  {id:'f2', title:'Pedro Paramo', reason:'11 days late', cents:385, date:'Jul 11, 2026'}
];

const CARDS_SEED = [
  {id:'c1', brand:'Visa', last4:'4242', exp:'09/28', holder:'Francisco Torreglosa', primary:true},
  {id:'c2', brand:'Mastercard', last4:'8817', exp:'04/27', holder:'Francisco Torreglosa', primary:false}
];

const NOTE_KINDS = {
  due:      {family:'due',      icon:'schedule',          fg:'#B3261E', bg:'rgba(179,38,30,.12)'},
  pending:  {family:'payments', icon:'storefront',        fg:'#8A6A28', bg:'rgba(224,166,60,.16)'},
  paid:     {family:'payments', icon:'payments',          fg:'#0F7A63', bg:'rgba(15,122,99,.12)'},
  transit:  {family:'returns',  icon:'local_shipping',    fg:'#0E5A6E', bg:'rgba(14,90,110,.12)'},
  returned: {family:'returns',  icon:'assignment_turned_in', fg:'#0F7A63', bg:'rgba(15,122,99,.12)'},
  hold:     {family:'holds',    icon:'bookmark_added',    fg:'#0E5A6E', bg:'rgba(14,90,110,.12)'},
  desk:     {family:'payments', icon:'confirmation_number', fg:'#8A6A28', bg:'rgba(224,166,60,.16)'},
  support:  {family:'support',  icon:'support_agent',      fg:'#0E5A6E', bg:'rgba(14,90,110,.12)'}
};

const TICKET_CATS = ['Payments and fines', 'Reservations and returns', 'Catalogue and availability', 'Account and plan', 'Something is broken'];

const TICKET_STATUS = {
  created:  ['Created',   'rgba(224,166,60,.20)', '#8A6A28', 'fiber_new'],
  review:   ['In review', 'rgba(14,90,110,.14)',  '#0E5A6E', 'hourglass_top'],
  resolved: ['Resolved',  'rgba(15,122,99,.14)',  '#0F7A63', 'task_alt']
};

const TICKETS_SEED = [
  {id:'TCK-2038', user:'Francisco Torreglosa', email:'fjtorreglosaa@gmail.com', self:true,
   subject:'The fine for Klara and the Sun was charged twice',
   category:'Payments and fines', library:'New York — Midtown',
   status:'resolved', owner:'Marcus Reed', created:'Aug 6, 2026', updated:'Aug 8, 2026',
   rating:5, review:'Marcus found the duplicate in minutes and refunded it the same day.',
   msgs:[
     {who:'member', name:'Francisco Torreglosa', time:'Aug 6, 2026 · 09:12', text:'I paid $2.10 on Jul 28 and the same fine shows again on my account.'},
     {who:'agent', name:'Marcus Reed', time:'Aug 6, 2026 · 11:40', text:'Thanks for the detail. I can see two entries with the same receipt. I am reversing the second one now.'},
     {who:'agent', name:'Marcus Reed', time:'Aug 8, 2026 · 08:05', text:'The duplicate is reversed and your balance is correct. Closing this as resolved.'}
   ]},
  {id:'TCK-2039', user:'Francisco Torreglosa', email:'fjtorreglosaa@gmail.com', self:true,
   subject:'Courier never came for The Savage Detectives',
   category:'Reservations and returns', library:'New York — Midtown',
   status:'review', owner:'Marcus Reed', created:'Aug 13, 2026', updated:'Aug 14, 2026',
   rating:0, review:'',
   msgs:[
     {who:'member', name:'Francisco Torreglosa', time:'Aug 13, 2026 · 18:30', text:'I booked a courier pickup for Aug 12 and nobody arrived. The book is now overdue and the fine keeps growing.'},
     {who:'agent', name:'Marcus Reed', time:'Aug 14, 2026 · 09:15', text:'I am checking with the courier company today and I will freeze the fine while we look into it.'}
   ]},
  {id:'TCK-2040', user:'Amara Osei', email:'amara.osei@astrolabe.co', self:false,
   subject:'Cannot see Max titles after upgrading',
   category:'Account and plan', library:'New York — Harlem',
   status:'created', owner:'', created:'Aug 15, 2026', updated:'Aug 15, 2026',
   rating:0, review:'',
   msgs:[
     {who:'member', name:'Amara Osei', time:'Aug 15, 2026 · 07:48', text:'I upgraded to Max yesterday but the catalogue still locks the Max titles for me.'}
   ]}
];



const NOTES_SEED = [
  {id:'n1', kind:'due', title:'The Savage Detectives is 20 days overdue',
   body:'The fine is $7.00 and grows $0.35 a day. Return it by courier or at the desk.', time:'2 h ago', read:false, route:'loans'},
  {id:'n2', kind:'paid', title:'Payment received — $2.10',
   body:'Late fine for Klara and the Sun. Receipt RC-20260728 is in your payment history.', time:'Yesterday', read:false, route:'fines'},
  {id:'n3', kind:'returned', title:'Papyrus is back in the catalog',
   body:'The librarian checked the copy in. Nothing else is pending on this reservation.', time:'Jul 30', read:true, route:'loans'}
];

const PAYMENTS_SEED = [
  {id:'p1', date:'Jul 28, 2026', desc:'Late fine — Klara and the Sun', method:'Visa •••• 4242', amount:'$2.10', receipt:'RC-20260728'},
  {id:'p2', date:'Jun 14, 2026', desc:'Home delivery — Sapiens', method:'Visa •••• 4242', amount:'$3.99', receipt:'RC-20260614'}
];

const EMPTY_CARD = {holder:'', number:'', exp:'', cvc:'', zip:'', primary:false};

const USER_STATUS = {
  active:{label:'Active', bg:'rgba(16,168,140,.14)', fg:'#0C7F70', icon:'check_circle'},
  pending:{label:'Pending verification', bg:'rgba(224,166,60,.20)', fg:'#8A6A28', icon:'mark_email_unread'},
  blocked:{label:'Blocked', bg:'rgba(179,38,30,.12)', fg:'#B3261E', icon:'block'},
  deleted:{label:'Deleted', bg:'rgba(16,38,46,.08)', fg:'#5C7480', icon:'person_off'}
};

const USERS = [
  {id:'u1', name:'Francisco Torreglosa', email:'fjtorreglosaa@gmail.com', role:'Plus', status:'active', city:'New York', library:'New York — Midtown', joined:'Mar 2021', last:'Today', loans:4, fines:'$7.00', purchases:11, onTime:'92%'},
  {id:'u2', name:'Dana Whitfield', email:'dana@astrolabe.co', role:'Admin', status:'active', city:'New York', library:'New York — Midtown', joined:'Jan 2023', last:'Today', loans:0, fines:'$0.00', purchases:0, onTime:'—'},
  {id:'u3', name:'Marcus Oyelaran', email:'marcus@astrolabe.co', role:'Admin', status:'active', city:'Chicago', library:'Chicago — Loop', joined:'Sep 2024', last:'Yesterday', loans:0, fines:'$0.00', purchases:2, onTime:'—'},
  {id:'u4', name:'Priya Raman', email:'priya@astrolabe.co', role:'Admin', status:'active', city:'Austin', library:'Austin — Mueller', joined:'Feb 2025', last:'3 days ago', loans:1, fines:'$0.00', purchases:0, onTime:'100%'},
  {id:'u5', name:'Alice Nakamura', email:'alice.n@fastmail.com', role:'Max', status:'active', city:'New York', library:'New York — Harlem', joined:'Jun 2022', last:'Today', loans:6, fines:'$0.00', purchases:24, onTime:'98%'},
  {id:'u6', name:'Tomás Iriarte', email:'t.iriarte@correo.mx', role:'Basic', status:'active', city:'Chicago', library:'Chicago — Pilsen', joined:'Nov 2025', last:'2 weeks ago', loans:1, fines:'$0.00', purchases:0, onTime:'100%'},
  {id:'u7', name:'Grace Abbott', email:'grace.abbott@mail.com', role:'Plus', status:'blocked', city:'Austin', library:'Austin — Mueller', joined:'Apr 2023', last:'Jul 2, 2026', loans:2, fines:'$46.20', purchases:3, onTime:'54%'},
  {id:'u8', name:'Yusuf Demir', email:'yusuf.demir@mail.com', role:'Max', status:'active', city:'New York', library:'New York — Midtown', joined:'Aug 2024', last:'4 days ago', loans:3, fines:'$0.00', purchases:9, onTime:'96%'},
  {id:'u9', name:'Rosa Lindqvist', email:'rosa.l@post.se', role:'Basic', status:'pending', city:'Chicago', library:'Chicago — Loop', joined:'Aug 2026', last:'Never', loans:0, fines:'$0.00', purchases:0, onTime:'—'},
  {id:'u10', name:'Elias Brandt', email:'elias.brandt@mail.de', role:'Plus', status:'deleted', city:'Austin', library:'Austin — Mueller', joined:'May 2022', last:'Jan 8, 2026', loans:0, fines:'$0.00', purchases:6, onTime:'88%'},
  {id:'u11', name:'Nadia Haddad', email:'nadia.h@mail.com', role:'Plus', status:'active', city:'New York', library:'New York — Harlem', joined:'Oct 2023', last:'Today', loans:2, fines:'$3.85', purchases:5, onTime:'90%'},
  {id:'u12', name:'Kwame Boateng', email:'kwame.b@mail.com', role:'Basic', status:'active', city:'Chicago', library:'Chicago — Loop', joined:'Feb 2026', last:'Last week', loans:1, fines:'$0.00', purchases:1, onTime:'100%'},
  {id:'u13', name:'Sofia Marchetti', email:'sofia.m@posta.it', role:'Max', status:'blocked', city:'New York', library:'New York — Midtown', joined:'Dec 2021', last:'Jun 19, 2026', loans:0, fines:'$62.00', purchases:14, onTime:'61%'},
  {id:'u14', name:'Hana Suzuki', email:'hana.suzuki@mail.jp', role:'Plus', status:'active', city:'Austin', library:'Austin — Mueller', joined:'Jul 2025', last:'Yesterday', loans:2, fines:'$0.00', purchases:2, onTime:'100%'}
];

const EMPTY_BOOK = {title:'', author:'', isbn:'', genre:'Fiction', tier:'Plus', price:'', copies:'2', branch:'New York — Midtown', notes:'', cover:null, tint:''};

const PLANS = [
  {name:'Basic', price:'$0', per:'/ month', summary:'Borrow at one library, Basic catalog only', bullets:[
    'Browse every library in the network',
    'Borrowing at 1 library of your choice',
    'Titles included in the Basic catalog',
    'No purchase discounts'
  ]},
  {name:'Plus', price:'$6.99', per:'/ month', summary:'Borrow across your city, full catalog', bullets:[
    'Everything in Basic',
    'Borrowing at every library in your city',
    'Full catalog with no restrictions',
    'Purchase discounts within your city'
  ]},
  {name:'Max', price:'$12.99', per:'/ month', summary:'Borrow platform-wide, discounts and points', bullets:[
    'Everything in Plus',
    'Borrowing at every library on the platform',
    'Purchase discounts in every city',
    'Points on every purchase, redeemable for books'
  ]}
];

class Component extends DCLogic {
  state = {
    view:'login', reg:{name:'', email:'', pass:'', country:'United States', city:'New York', plan:'Plus', terms:false},
    logged:false, email:'', pass:'', route:'home', dark:false, menuOpen:false,
    query:'', filter:'All', provider:'Claude', apiKey:'', keySaved:false,
    aiConfig:{'New York — Midtown':{provider:'Claude', key:'sk-ant-api03-••••4Q2', on:true}},
    aiDraft:{},
    detailId:null, snack:null, deleted:[], fabOpen:false,
    planModal:null, planPending:null,
    prefs:{delivery:'pickup', ret:'courier', purchase:'pickup'}, buyModal:null, dragCover:false,
    reserveId:null, reserveCopy:0, reserveDelivery:'pickup', added:[],
    returned:['l5'], toggles:{due:true, promos:false, holds:true, digest:true},
    notes:NOTES_SEED.slice(), notesOpen:false, notifOn:true, notifSound:true,
    notifKinds:{due:true, payments:true, returns:true, holds:true, support:true},
    tickets:TICKETS_SEED.slice(), ticketOpen:null, ticketNew:null, ticketReply:'',
    ticketStars:0, ticketReview:'', ticketFilter:'All',
    reviews:{}, rateFor:null, rateStars:0, rateText:'',
    manuals:MANUALS_SEED.slice(), pendingFines:[], manualFilter:'Awaiting validation',
    manualAction:null, manualNew:null, manualQuery:'',
    sort:{loans:{k:'due',d:'asc'}, ledger:{k:'date',d:'desc'}, books:{k:'title',d:'asc'}, users:{k:'name',d:'asc'}, catalog:{k:'title',d:'asc'}},
    page:{loans:0, ledger:0, books:0, users:0, catalog:0},
    rpp:{loans:5, ledger:5, books:5, users:8, catalog:8},
    tq:{loans:'', ledger:'', books:'', users:'', tickets:''},
    userFilter:'All', userStatus:{}, userDetail:null, userAction:null,
    catalogView:'grid', libsView:'cards', supportView:'cards',
    paidFines:[], cards:CARDS_SEED.slice(), payments:PAYMENTS_SEED.slice(),
    payModal:null, cardModal:null, cardRemove:null, cardExit:false,
    navOpen:true, busy:null, fabDocked:false,
    role:'Plus', deskRole:'member', inTransit:[], codeFor:null, codeInput:'',
    bookStatus:{}, newBooks:[], nextId:1000, adminFilter:'All',
    wiz:null, wizExit:false, bookAction:null, bookExit:false,
    invite:null, inviteExit:false, invites:[],
    bookEdits:{}, repairInfo:{}, deleteInfo:{},
    loading:{loans:true, ledger:true, books:true, catalog:true, users:true, stats:true, recos:true, profile:true, manuals:true, tickets:true}
  };

  componentDidMount(){
    this.load('loans', 900); this.load('catalog', 1100); this.load('stats', 1000); this.load('recos', 1400);
    try {
      const v = window.localStorage.getItem('astrolabe.catalogView');
      if(v==='grid' || v==='table') this.setState({catalogView:v});
      const lv = window.localStorage.getItem('astrolabe.libsView');
      if(lv==='cards' || lv==='table') this.setState({libsView:lv});
      const sv = window.localStorage.getItem('astrolabe.supportView');
      if(sv==='cards' || sv==='table') this.setState({supportView:sv});
    } catch(e){}
  }

  setLibsView(v){
    try { window.localStorage.setItem('astrolabe.libsView', v); } catch(e){}
    this.setState({libsView:v});
  }

  setSupportView(v){
    try { window.localStorage.setItem('astrolabe.supportView', v); } catch(e){}
    this.setState({supportView:v});
  }

  setCatalogView(v){
    try { window.localStorage.setItem('astrolabe.catalogView', v); } catch(e){}
    this.setState({catalogView:v});
  }

  load(key, ms){
    this._lt = this._lt || {};
    clearTimeout(this._lt[key]);
    this.setState(st=>({loading:Object.assign({}, st.loading, {[key]:true})}));
    this._lt[key] = setTimeout(()=>this.setState(st=>({loading:Object.assign({}, st.loading, {[key]:false})})), ms || 800);
  }

  readCover(file, apply){
    if(!file) return;
    if(!/^image\//.test(file.type)) return this.snack('That file is not an image. Use JPG, PNG or WebP.', 'error');
    if(file.size > 4 * 1024 * 1024) return this.snack('That image is larger than 4 MB. Pick a smaller file.', 'error');
    const r = new FileReader();
    r.onload = () => apply(r.result);
    r.readAsDataURL(file);
  }
  coverBox(d, patch, t){
    const tint = d.tint || TINTS[0];
    const drag = this.state.dragCover;
    return {
      cover: d.cover || '', hasCover: !!d.cover, noCover: !d.cover, tint,
      coverBg: d.cover ? 'url("' + d.cover + '")' : 'none',
      dropBg: drag ? 'rgba(12,127,112,.10)' : 'transparent',
      dropBorder: drag ? '#0C7F70' : t.field,
      onFile: e => { const file = e.target.files && e.target.files[0];
        this.readCover(file, url => patch({cover:url})); e.target.value = ''; },
      onDragOver: e => { e.preventDefault(); if(!this.state.dragCover) this.setState({dragCover:true}); },
      onDragLeave: e => { e.preventDefault(); this.setState({dragCover:false}); },
      onDrop: e => { e.preventDefault(); this.setState({dragCover:false});
        const file = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
        this.readCover(file, url => patch({cover:url})); },
      remove: () => patch({cover:null}),
      swatches: TINTS.map(c => ({color:c,
        border: (!d.cover && tint===c) ? '#10262E' : 'transparent',
        go: () => patch({tint:c, cover:null})}))
    };
  }
  loadFor(route){
    const map = {home:['loans','stats','recos'], loans:['loans'], catalog:['catalog'], ai:['recos'],
      support:['tickets'], 'admin-support':['tickets'],
      profile:['ledger','profile'], fines:['ledger'], purchases:['ledger','profile'],
      'admin-payments':['manuals'],
      'admin-books':['books'], 'admin-users':['users']};
    (map[route] || []).forEach((k,i)=>this.load(k, 700 + i*260));
  }

  ding(){
    if(!this.state.notifSound) return;
    try {
      const AC = window.AudioContext || window.webkitAudioContext;
      if(!AC) return;
      this._ac = this._ac || new AC();
      const ac = this._ac;
      if(ac.state === 'suspended' && ac.resume) ac.resume();
      [[880, 0], [1174, 0.11]].forEach(([hz, at]) => {
        const o = ac.createOscillator(), g = ac.createGain(), t0 = ac.currentTime + at;
        o.type = 'sine'; o.frequency.value = hz;
        g.gain.setValueAtTime(0.0001, t0);
        g.gain.exponentialRampToValueAtTime(0.16, t0 + 0.015);
        g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.16);
        o.connect(g); g.connect(ac.destination); o.start(t0); o.stop(t0 + 0.18);
      });
    } catch(e){}
  }

  notify(kind, title, body, route){
    const s = this.state;
    const def = NOTE_KINDS[kind] || NOTE_KINDS.paid;
    if(!s.notifOn || !s.notifKinds[def.family]) return;
    const note = {id:'n' + Date.now() + Math.round(Math.random()*999), kind:kind,
      title:title, body:body, time:'Just now', read:false, route:route || 'home'};
    this.setState({notes:[note].concat(this.state.notes)});
    this.ding();
  }

  copier(value, label){
    return () => {
      const text = String(value || '');
      if(!text) return;
      const ok = () => this.snack((label || 'Code') + ' ' + text + ' copied to your clipboard.', 'ok');
      const legacy = () => {
        try {
          const ta = document.createElement('textarea');
          ta.value = text; ta.setAttribute('readonly',''); ta.style.position='fixed'; ta.style.top='0'; ta.style.opacity='0';
          document.body.appendChild(ta); ta.focus(); ta.select();
          const done = document.execCommand('copy');
          document.body.removeChild(ta);
          if(done) return ok();
        } catch(e){}
        this.snack('Your browser blocked the clipboard. Select ' + text + ' and copy it manually.', 'error');
      };
      if(navigator.clipboard && navigator.clipboard.writeText){
        navigator.clipboard.writeText(text).then(ok, legacy);
        return;
      }
      legacy();
    };
  }

  run(key, ms, done){
    if(this.state.busy) return;
    this.setState({busy:key});
    this._bt = setTimeout(()=>{ this.setState({busy:null}); if(done) done(); }, ms || 800);
  }

  upsertBook(status){
    const w = this.state.wiz, d = w.data;
    const id = w.id || this.state.nextId;
    const price = String(d.price || '').replace(/[^0-9.]/g,'');
    const rec = {
      id:id, title:d.title || 'Untitled draft', author:d.author || 'Unknown author',
      genre:d.genre, isbn:d.isbn || '—', year:2026, pages:0,
      price: price ? '$' + Number(price).toFixed(2) : '$0.00',
      rating:'—', copies:(d.copies||'0') + ' / ' + (d.copies||'0'),
      branch:d.branch, blurb:d.notes || '', tier:d.tier, status:status,
      cover:d.cover || null, tint:d.tint || ''
    };
    const exists = this.state.newBooks.filter(b=>b.id===id).length > 0;
    this.setState({
      newBooks: exists ? this.state.newBooks.map(b=>b.id===id?rec:b) : this.state.newBooks.concat(rec),
      nextId: w.id ? this.state.nextId : this.state.nextId + 1,
      bookStatus: Object.assign({}, this.state.bookStatus, {[id]:status}),
      wiz:null, wizExit:false
    });
    return rec;
  }

  pickupCode(id){
    let h = 0;
    for(let i=0;i<String(id).length;i++) h = (h*31 + String(id).charCodeAt(i)) % 9000;
    return 'PU-' + (1000 + h);
  }

  cmp(a,b,d){ const x = typeof a==='string' ? a.localeCompare(b,'en') : (a<b?-1:a>b?1:0); return d==='asc' ? x : -x; }

  sortRows(list, tbl, cols){
    const st = this.state.sort[tbl], col = cols.filter(c=>c.k===st.k)[0];
    const get = col && col.get ? col.get : (r=>r[st.k]);
    return list.slice().sort((a,b)=>this.cmp(get(a), get(b), st.d));
  }

  headers(tbl, cols){
    const st = this.state.sort[tbl], t = this.theme();
    return cols.map(c=>({
      label:c.label, pad:c.pad || '12px 12px', align:c.align || 'left',
      icon: st.k===c.k ? (st.d==='asc' ? 'arrow_upward' : 'arrow_downward') : 'unfold_more',
      op: st.k===c.k ? 1 : 0.32,
      color: st.k===c.k ? '#0E5A6E' : t.dim,
      go: ()=>this.setState({
        sort:Object.assign({}, this.state.sort, {[tbl]:{k:c.k, d: st.k===c.k && st.d==='asc' ? 'desc' : 'asc'}}),
        page:Object.assign({}, this.state.page, {[tbl]:0})
      })
    }));
  }

  pager(tbl, total){
    const rpp = this.state.rpp[tbl], pages = Math.max(1, Math.ceil(total/rpp));
    const p = Math.min(this.state.page[tbl], pages-1);
    const set = n => this.setState({page:Object.assign({}, this.state.page, {[tbl]:n})});
    return {
      info: (total ? (p*rpp+1) : 0) + '–' + Math.min(total, (p+1)*rpp) + ' of ' + total,
      pageInfo: 'Page ' + (p+1) + ' of ' + pages,
      rpp: String(rpp),
      onRpp: e=>this.setState({rpp:Object.assign({}, this.state.rpp, {[tbl]:Number(e.target.value)}), page:Object.assign({}, this.state.page, {[tbl]:0})}),
      prev: ()=>set(Math.max(0, p-1)), next: ()=>set(Math.min(pages-1, p+1)),
      prevOp: p===0 ? 0.32 : 1, nextOp: p>=pages-1 ? 0.32 : 1,
      start: p*rpp, end: (p+1)*rpp
    };
  }

  tableSearch(tbl, placeholder){
    return {value:this.state.tq[tbl], placeholder:placeholder,
      onInput:e=>this.setState({tq:Object.assign({}, this.state.tq, {[tbl]:e.target.value}), page:Object.assign({}, this.state.page, {[tbl]:0})})};
  }

  match(row, fields, q){
    if(!q) return true;
    const n = q.trim().toLowerCase();
    return fields.map(f=>String(row[f]||'')).join(' ').toLowerCase().indexOf(n) > -1;
  }

  componentWillUnmount(){
    clearTimeout(this._st); clearTimeout(this._bt);
    Object.keys(this._lt || {}).forEach(k=>clearTimeout(this._lt[k]));
  }

  snack(msg, kind){
    const bg = kind==='error' ? '#B3261E' : kind==='ok' ? '#0F7A63' : '#10262E';
    const icon = kind==='error' ? 'error' : kind==='ok' ? 'check_circle' : 'info';
    this.setState({snack:{msg, bg, icon}});
    clearTimeout(this._st);
    this._st = setTimeout(()=>this.setState({snack:null}), 5000);
  }

  theme(){
    return this.state.dark
      ? {bg:'#0B1519', surface:'#10222A', text:'#E8F3F6', dim:'#93AFB9', border:'rgba(255,255,255,.12)', field:'rgba(255,255,255,.28)',
         skel:'linear-gradient(90deg,rgba(255,255,255,.06),rgba(255,255,255,.16),rgba(255,255,255,.06))', scheme:'dark'}
      : {bg:'#F4F9FB', surface:'#FFFFFF', text:'#10262E', dim:'#5C7480', border:'rgba(16,38,46,.12)', field:'rgba(16,38,46,.26)',
         skel:'linear-gradient(90deg,rgba(16,38,46,.07),rgba(16,38,46,.16),rgba(16,38,46,.07))', scheme:'light'};
  }

  renderVals(){
    const s = this.state, t = this.theme(), P = '#0E5A6E';
    const go = r => () => { this.setState({route:r, menuOpen:false, fabOpen:false}); this.loadFor(r); };
    const busyOn = k => s.busy===k;
    const roleDef = ROLES.filter(r=>r.key===s.role)[0] || ROLES[1];
    const isMember = roleDef.kind==='member';
    const isSuper = roleDef.kind==='super';
    const isStaff = !isMember;
    const scope = isSuper ? LIBRARIES : ADMIN_SCOPE;
    const plan = isMember ? s.role : 'Max';
    const aiPlan = !isMember || plan !== 'Basic';
    const cfgOf = lib => s.aiConfig[lib] || null;
    const liveLibs = LIBRARIES.filter(l => { const c = cfgOf(l); return c && c.on && c.key; });
    const myCity = s.reg.city, myHome = HOME_BRANCH[myCity] || '';

    const setReg = patch => this.setState({reg:Object.assign({}, s.reg, patch)});

    const navRaw = [
      {header:true, label:'Discover', member:true},
      {icon:'space_dashboard', label:'Home', route:'home', member:true},
      {icon:'menu_book', label:'Catalog', route:'catalog', member:true},
      {icon:'auto_awesome', label:'AI recommendations', route:'ai', pro:true, member:true},
      {header:true, label:'My account', member:true},
      {icon:'bookmarks', label:'Book Reservations', route:'loans', badge:'2', member:true},
      {icon:'receipt_long', label:'Fines & payments', route:'fines', member:true},
      {icon:'shopping_bag', label:'My purchases', route:'purchases', member:true},
      {icon:'support_agent', label:'Help & support', route:'support', member:true},
      {header:true, label:'Administration', staff:true},
      {icon:'group', label:'Users', route:'admin-users', staff:true},
      {icon:'library_add', label:'Book management', route:'admin-books', staff:true},
      {icon:'point_of_sale', label:'Manual payments', route:'admin-payments', staff:true},
      {icon:'contact_support', label:'Support tickets', route:'admin-support', staff:true},
      {icon:'admin_panel_settings', label:'Libraries & admins', route:'admin-libraries', super:true}
    ].filter(n => (!n.staff || isStaff) && (!n.super || isSuper) && (!n.pro || aiPlan) && (!n.member || !isStaff));
    const nav = navRaw.map((n,i) => n.header
      ? {header:true, link:false, label:n.label, key:i}
      : {header:false, link:true, label:n.label, icon:n.icon, badge:n.badge||null, go:go(n.route),
         bg: s.route===n.route ? 'rgba(14,90,110,.14)' : 'transparent',
         fg: s.route===n.route ? P : t.text,
         weight: s.route===n.route ? 600 : 500});

    const titles = {home:'Home', catalog:'Catalog', ai:'AI recommendations', loans:'Book Reservations',
      profile:'My profile', fines:'Fines & payments', purchases:'My purchases', settings:'Settings',
      'admin-books':'Book management', 'admin-users':'Users', 'admin-libraries':'Libraries & admins',
      'admin-payments':'Manual payments', support:'Help & support', 'admin-support':'Support tickets'};

    const statusOf = b => s.bookStatus[b.id] || b.status || 'catalog';
    const ALL = BOOKS.concat(s.newBooks).map(b => Object.assign({}, b, s.bookEdits[b.id] || {}, {status: statusOf(b)}));

    const q = s.query.trim().toLowerCase();
    const books = ALL.filter(b => b.status==='catalog')
      .filter(b => s.filter==='All' || b.genre===s.filter)
      .filter(b => !q || (b.title+b.author+b.isbn+b.genre).toLowerCase().includes(q));


    const copyState = (tier, st) => {
      if(st.n <= 0) return {ok:false, reason:'All copies out'};
      if(plan==='Basic'){
        if(tier!=='Basic') return {ok:false, reason:'Not in Basic catalog'};
        if(!(st.city===myCity && st.branch===myHome)) return {ok:false, reason:'Basic borrows at ' + (myHome||'your home library') + ' only'};
      } else if(plan==='Plus'){
        if(st.city!==myCity) return {ok:false, reason:'Outside ' + myCity};
      }
      return {ok:true, reason:''};
    };

    const bookAccess = b => {
      const m = META[b.id] || {tier: b.tier || 'Basic', stock:[{
        city: String(b.branch).split(' — ')[0],
        branch: String(b.branch).split(' — ')[1] || b.branch,
        n: parseInt(b.copies, 10) || 0
      }]};
      const rows = m.stock.map(st => Object.assign({}, st, copyState(m.tier, st)));
      const usable = rows.filter(r=>r.ok);
      let badge = null;
      if(plan==='Basic' && m.tier!=='Basic') badge = 'Not in Basic plan';
      else if(!usable.length){
        badge = rows.some(r=>r.n>0)
          ? (plan==='Basic' ? 'Home library only' : plan==='Plus' ? 'Not in ' + myCity : 'Unavailable')
          : 'All copies out';
      }
      return {tier:m.tier, rows, can:usable.length>0, badge};
    };

    const shown = books.map(b => {
      const a = bookAccess(b);
      return Object.assign({}, b, {
        tint: b.tint || TINTS[(Number(String(b.id).replace(/\D/g,'')) - 1) % TINTS.length],
        cover: b.cover || '', hasCover: !!b.cover, coverBg: b.cover ? 'url("' + b.cover + '")' : 'none',
        tier: a.tier, canBorrow: a.can, badge: a.badge, hasBadge: !!a.badge,
        availability: b.copies.charAt(0)==='0' ? 'No copies left' : b.copies + ' available',
        btnBg: a.can ? '#0E5A6E' : 'transparent',
        btnFg: a.can ? '#fff' : t.dim,
        btnBorder: a.can ? '#0E5A6E' : t.field,
        btnLabel: a.can ? 'Reserve' : 'Unavailable',
        btnCursor: a.can ? 'pointer' : 'not-allowed',
        onBorrow: () => a.can
          ? this.setState({reserveId:b.id, reserveCopy:a.rows.indexOf(a.rows.filter(r=>r.ok)[0]), reserveDelivery:s.prefs.delivery, detailId:null})
          : this.snack(a.badge + ' — “' + b.title + '” cannot be reserved on the ' + plan + ' plan.', 'error'),
        onOpen: () => this.setState({detailId:b.id}),
        deleting: s.busy==='del:'+b.id, idle: s.busy!=='del:'+b.id,
        onDelete: () => this.run('del:'+b.id, 800, ()=>{ this.setState({deleted:s.deleted.concat(b.id)}); this.snack('"'+b.title+'" was removed from the catalog.', 'info'); })
      });
    });

    const catalogCols = [
      {k:'title', label:'Title', pad:'14px 20px'},
      {k:'author', label:'Author'},
      {k:'genre', label:'Genre'},
      {k:'tier', label:'Plan'},
      {k:'availability', label:'Availability'},
      {k:'rating', label:'Rating', get:r=>Number(r.rating) || 0},
      {k:'price', label:'Price', get:r=>Number(String(r.price).replace(/[^0-9.]/g,'')) || 0}
    ];
    const catalogSorted = this.sortRows(shown, 'catalog', catalogCols);
    const catalogPager = this.pager('catalog', catalogSorted.length);
    const catalogRows = catalogSorted.slice(catalogPager.start, catalogPager.end);

    const genres = ['All'].concat(Array.from(new Set(BOOKS.map(b=>b.genre))));
    const filters = genres.map(g => ({
      label:g, go:()=>this.setState({filter:g}),
      bg: s.filter===g ? 'rgba(14,90,110,.14)' : 'transparent',
      fg: s.filter===g ? P : t.text,
      border: s.filter===g ? P : t.field
    }));

    const loanData = [
      {id:'l1', title:'The Savage Detectives', author:'Roberto Bolano', from:'Jul 12, 2026', due:'Jul 26, 2026', dueTs:20260726, fromTs:20260712, delivery:'Home delivery', days:20},
      {id:'l2', title:'Discipline and Punish', author:'Michel Foucault', from:'Aug 2, 2026', due:'Aug 16, 2026', dueTs:20260816, fromTs:20260802, delivery:'Pickup — Midtown', days:0},
      {id:'l3', title:'Klara and the Sun', author:'Kazuo Ishiguro', from:'Aug 8, 2026', due:'Aug 22, 2026', dueTs:20260822, fromTs:20260808, delivery:'Home delivery', days:0},
      {id:'l4', title:'Sapiens', author:'Yuval Noah Harari', from:'Aug 5, 2026', due:'Aug 19, 2026', dueTs:20260819, fromTs:20260805, delivery:'Pickup — Mueller', days:0},
      {id:'l5', title:'Papyrus', author:'Irene Vallejo', from:'Aug 1, 2026', due:'Aug 15, 2026', dueTs:20260815, fromTs:20260801, delivery:'Pickup — Mueller', days:0},
      {id:'l6', title:'Pedro Paramo', author:'Juan Rulfo', from:'Jul 21, 2026', due:'Aug 4, 2026', dueTs:20260804, fromTs:20260721, delivery:'Home delivery', days:11}
    ];
    const isLibrarian = isStaff && s.deskRole==='librarian';
    const loansAll = s.added.concat(loanData).map(l => {
      const done = s.returned.indexOf(l.id) > -1;
      const transit = !done && s.inTransit.indexOf(l.id) > -1;
      const late = !done && !transit && l.days > 0;
      const status = done ? 'Returned' : transit ? 'Return in progress' : late ? 'Overdue · '+l.days+' days' : 'Reserved';
      const chip = done ? ['rgba(15,122,99,.14)','#0F7A63']
        : transit ? ['rgba(224,166,60,.20)','#8A6A28']
        : late ? ['rgba(179,38,30,.12)','#B3261E']
        : ['rgba(16,168,140,.12)','#0C7F70'];

      let action, live = true, run;
      if(isLibrarian){
        if(done){ action = 'Received'; live = false; run = ()=>this.snack('“'+l.title+'” is already back on the shelf.', 'info'); }
        else { action = transit ? 'Check in parcel' : 'Receive book';
          run = ()=>this.run('row:'+l.id, 900, ()=>{
            this.setState({returned:s.returned.concat(l.id), inTransit:s.inTransit.filter(x=>x!==l.id)});
            this.snack(late
              ? 'Checked in “'+l.title+'”. A $7.00 late fine was charged to the member.'
              : 'Checked in “'+l.title+'”. Copy is back on the shelf.', late ? 'info' : 'ok');
            this.notify('returned', 'Return confirmed — “'+l.title+'”',
              late ? 'The library received the copy. A $7.00 late fine is now on your account.'
                   : 'The library received the copy on time. Nothing else is pending on this reservation.', 'loans');
          });
        }
      } else {
        if(done){ action = 'Reserve again'; run = ()=>this.run('row:'+l.id, 900, ()=>this.snack('“'+l.title+'” reserved again for 14 days.', 'ok')); }
        else if(transit){ action = 'With courier'; live = false;
          run = ()=>this.snack('“'+l.title+'” is on its way back. The library marks it Returned on arrival.', 'info'); }
        else { action = s.prefs.ret==='branch' ? 'Return at library' : 'Return by courier';
          run = ()=>this.setState({codeFor:l.id, codeInput:''}); }
      }

      const rv = s.reviews[l.id];
      return Object.assign({}, l, {
        status, chipBg:chip[0], chipFg:chip[1],
        action, onReturn:run,
        canRate: done && !isLibrarian,
        rateLabel: rv ? 'Edit review' : 'Rate',
        rateIcon: rv ? 'star' : 'star_border',
        rateColor: rv ? '#E0A63C' : t.dim,
        rateNote: rv ? 'You rated it ' + rv.stars + '/5' : '',
        hasReview: !!rv,
        onRate: ()=>this.setState({rateFor:l.id, rateStars: rv ? rv.stars : 0, rateText: rv ? rv.text : ''}),
        actBorder: live ? t.field : t.border,
        actFg: live ? t.text : t.dim,
        actCursor: live ? 'pointer' : 'default',
        busy: s.busy==='row:'+l.id, idle: s.busy!=='row:'+l.id
      });
    });

    const rateVals = (function(){
      if(!s.rateFor) return null;
      const l = loansAll.filter(x=>x.id===s.rateFor)[0];
      if(!l) return null;
      const existing = s.reviews[s.rateFor];
      const bk = BOOKS.filter(x=>x.title===l.title)[0];
      const LABELS = ['Tap a star to rate', 'Not for me', 'It was fine', 'Good read', 'Really good', 'Loved it'];
      return {
        title:l.title, author:l.author, tint: (bk ? TINTS[(bk.id-1)%TINTS.length] : '#0B2E3B'),
        heading: existing ? 'Edit your review' : 'How was this book?',
        intro: 'You returned this copy on ' + l.due + '. Your rating helps other members and improves your recommendations.',
        stars: s.rateStars, label: LABELS[s.rateStars] || LABELS[0],
        items: [1,2,3,4,5].map(i => ({
          icon: i <= s.rateStars ? 'star' : 'star_border',
          fill: i <= s.rateStars ? '"FILL" 1' : '"FILL" 0',
          color: i <= s.rateStars ? '#E0A63C' : t.dim,
          aria: i + ' of 5 stars',
          set: ()=>this.setState({rateStars:i})
        })),
        text:s.rateText, count: s.rateText.length + ' / 500',
        onText: e=>this.setState({rateText:String(e.target.value).slice(0,500)}),
        canRemove: !!existing,
        busy: s.busy==='rate', idle: s.busy!=='rate',
        save: ()=>{
          if(!s.rateStars){ this.snack('Pick a star rating before you publish.', 'error'); return; }
          const next = Object.assign({}, s.reviews);
          next[s.rateFor] = {stars:s.rateStars, text:s.rateText, date:'Aug 15, 2026'};
          this.run('rate', 850, ()=>{
            this.setState({reviews:next, rateFor:null, rateStars:0, rateText:''});
            this.snack(existing ? 'Your review was updated.' : 'Thanks — your review of \u201C' + l.title + '\u201D is published.', 'ok');
          });
        },
        remove: ()=>{
          const next = Object.assign({}, s.reviews);
          delete next[s.rateFor];
          this.run('rate', 700, ()=>{
            this.setState({reviews:next, rateFor:null, rateStars:0, rateText:''});
            this.snack('Your review was removed.', 'info');
          });
        },
        close: ()=>this.setState({rateFor:null})
      };
    }).call(this);

    const resCols = [
      {k:'title', label:'Book', pad:'12px 20px'},
      {k:'from', label:'Borrowed', get:r=>r.fromTs},
      {k:'due', label:'Due', get:r=>r.dueTs},
      {k:'delivery', label:'Delivery'},
      {k:'status', label:'Status'},
      {k:'action', label:'Action', align:'right', pad:'12px 20px'}
    ];
    const loansFiltered = loansAll.filter(l=>this.match(l, ['title','author','status','delivery'], s.tq.loans));
    const loansSorted = this.sortRows(loansFiltered, 'loans', resCols);
    const loansPager = this.pager('loans', loansSorted.length);
    const loans = loansSorted.slice(loansPager.start, loansPager.end);

    const recoList = [
      {id:8, why:'Because you read two magical-realism novels this quarter.', match:'94% match'},
      {id:4, why:'Your topics include book history and narrative essay.', match:'91% match'},
      {id:12, why:'Similar in tone to Klara and the Sun, one of your reservations.', match:'87% match'},
      {id:9, why:'Biography, the genre you return on time most often.', match:'83% match'}
    ].map(r => {
      const b = BOOKS.filter(x=>x.id===r.id)[0];
      const shb = shown.filter(x=>x.id===b.id)[0];
      return {title:b.title, author:b.author, why:r.why, match:r.match,
        tint:(shb && shb.tint) || TINTS[(b.id-1)%TINTS.length],
        cover:(shb && shb.cover) || '', hasCover: !!(shb && shb.cover),
        coverBg: (shb && shb.cover) ? 'url("' + shb.cover + '")' : 'none',
        onBorrow:()=>{ const sh = shown.filter(x=>x.id===b.id)[0]; sh ? sh.onBorrow() : this.snack('“'+b.title+'” is not in the catalog anymore.','info'); }};
    });

    const num = v => Number(String(v).replace(/[^0-9.]/g,'')) || 0;
    const bookCols = [
      {k:'title', label:'Title', pad:'14px 20px'},
      {k:'author', label:'Author'},
      {k:'isbn', label:'ISBN'},
      {k:'genre', label:'Genre'},
      {k:'status', label:'Status', get:r=>r.statusLabel},
      {k:'copies', label:'Copies', get:r=>num(r.copies.split('/')[0])},
      {k:'price', label:'Price', get:r=>num(r.price)},
      {k:'branch', label:'Branch'}
    ];
    const adminBooks = ALL
      .filter(b => isSuper || scope.indexOf(b.branch) > -1)
      .filter(b => s.adminFilter==='All' || BOOK_STATUS[b.status].label===s.adminFilter)
      .map(b => {
        const st = BOOK_STATUS[b.status];
        const setStatus = (v, msg, kind) => () => this.run('row:'+b.id, 700, ()=>{
          this.setState({bookStatus:Object.assign({}, s.bookStatus, {[b.id]:v})});
          this.snack(msg, kind || 'ok');
        });
        return Object.assign({}, b, {
          tint: b.tint || TINTS[(Number(String(b.id).slice(-1))) % TINTS.length],
          cover: b.cover || '', hasCover: !!b.cover, coverBg: b.cover ? 'url("' + b.cover + '")' : 'none', coverBg: b.cover ? 'url("' + b.cover + '")' : 'none',
          statusLabel:st.label, statusBg:st.bg, statusFg:st.fg, statusIcon:st.icon,
          isDraft: b.status==='draft', isCatalog: b.status==='catalog',
          isRepair: b.status==='repair', isDeleted: b.status==='deleted',
          busy: s.busy==='row:'+b.id, idle: s.busy!=='row:'+b.id,
          copyIsbn: this.copier(b.isbn, 'ISBN'),
          onPublish: setStatus('catalog', '“'+b.title+'” is now in the catalog.'),
          onRepair: () => this.setState({bookAction:{kind:'repair', id:b.id, dirty:false, confirming:false,
            data:{reason:REPAIR_REASONS[0], notes:'', back:TODAY_ISO}}}),
          onRestore: setStatus('catalog', '“'+b.title+'” is back in the catalog.'),
          onRemove: () => this.setState({bookAction:{kind:'delete', id:b.id, dirty:false, confirming:false,
            data:{reason:DELETE_REASONS[0], notes:''}}}),
          statusNote: b.status==='repair' ? ((s.repairInfo[b.id]||{}).reason || '')
            : b.status==='deleted' ? ((s.deleteInfo[b.id]||{}).reason || '') : '',
          onEdit: () => b.status==='draft'
            ? this.setState({wiz:{step:0, id:b.id, data:Object.assign({}, EMPTY_BOOK, b), dirty:false}})
            : this.setState({bookAction:{kind:'edit', id:b.id, dirty:false, confirming:false, data:{
                title:b.title, author:b.author, isbn:b.isbn, genre:b.genre,
                price:String(b.price).replace('$',''), copies:String(b.copies).split(' / ')[0],
                branch:b.branch, tier:b.tier || 'Plus'
              }}})
        });
      });

    const booksFiltered = adminBooks.filter(b=>this.match(b, ['title','author','isbn','genre','branch'], s.tq.books));
    const booksSorted = this.sortRows(booksFiltered, 'books', bookCols);
    const booksPager = this.pager('books', booksSorted.length);
    const bookRows = booksSorted.slice(booksPager.start, booksPager.end);

    const ledgerData = [
      {date:'Aug 12, 2026', ts:20260812, concept:'Late fine — The Savage Detectives', kind:'Charge', amount:'-$7.00', color:'#B3261E'},
      {date:'Aug 8, 2026', ts:20260808, concept:'Reservation — Klara and the Sun', kind:'Reservation', amount:'$0.00', color:t.dim},
      {date:'Aug 3, 2026', ts:20260803, concept:'Purchase — Papyrus', kind:'Sale', amount:'-$26.00', color:t.text},
      {date:'Aug 3, 2026', ts:20260803, concept:'Home delivery', kind:'Service', amount:'-$3.99', color:t.text},
      {date:'Jul 28, 2026', ts:20260728, concept:'Wallet top-up', kind:'Credit', amount:'+$50.00', color:'#0F7A63'},
      {date:'Jul 19, 2026', ts:20260719, concept:'Purchase — Sapiens', kind:'Sale', amount:'-$23.00', color:t.text},
      {date:'Jul 11, 2026', ts:20260711, concept:'Late fine — Pedro Paramo', kind:'Charge', amount:'-$3.85', color:'#B3261E'}
    ];
    const ledgerCols = [
      {k:'date', label:'Date', pad:'12px 20px', get:r=>r.ts},
      {k:'concept', label:'Description'},
      {k:'kind', label:'Type'},
      {k:'amount', label:'Amount', align:'right', pad:'12px 20px', get:r=>num(r.amount) * (r.amount.charAt(0)==='-' ? -1 : 1)}
    ];
    const ledgerFiltered = ledgerData.filter(e=>this.match(e, ['date','concept','kind','amount'], s.tq.ledger));
    const ledgerSorted = this.sortRows(ledgerFiltered, 'ledger', ledgerCols);
    const ledgerPager = this.pager('ledger', ledgerSorted.length);
    const ledgerRows = ledgerSorted.slice(ledgerPager.start, ledgerPager.end);

    const userRows0 = USERS.concat(s.invites.map((i,n)=>({
        id:'inv'+n, name:i.name, email:i.email, role:i.role, status:'pending',
        city: String(i.libs[0]||'—').split(' — ')[0], library:i.libs[0]||'—',
        joined:i.sentAt, last:'Never', loans:0, fines:'$0.00', purchases:0, onTime:'—'
      })))
      .map(u => {
        const st = USER_STATUS[s.userStatus[u.id] || u.status];
        return Object.assign({}, u, {
          status: s.userStatus[u.id] || u.status,
          statusLabel:st.label, statusBg:st.bg, statusFg:st.fg, statusIcon:st.icon,
          initials: u.name.split(' ').map(x=>x.charAt(0)).slice(0,2).join(''),
          isStaffUser: u.role==='Admin' || u.role==='Super Admin',
          open:()=>this.setState({userDetail:u.id})
        });
      });

    const userCols = [
      {k:'name', label:'User', pad:'14px 20px'},
      {k:'email', label:'Email'},
      {k:'role', label:'Role'},
      {k:'statusLabel', label:'Status'},
      {k:'library', label:'Library'},
      {k:'joined', label:'Member since'}
    ];
    const usersFiltered = userRows0
      .filter(u => s.userFilter==='All' || u.statusLabel===s.userFilter)
      .filter(u => this.match(u, ['name','email','role','statusLabel','library','city'], s.tq.users));
    const usersSorted = this.sortRows(usersFiltered, 'users', userCols);
    const usersPager = this.pager('users', usersSorted.length);
    const userRows = usersSorted.slice(usersPager.start, usersPager.end);

    const money = c => '$' + (c/100).toFixed(2);
    const unpaidFines = FINES_SEED.filter(x=>s.paidFines.indexOf(x.id) < 0);
    const openFines = unpaidFines.filter(x=>s.pendingFines.indexOf(x.id) < 0);
    const dueCents = unpaidFines.reduce((n,x)=>n+x.cents, 0);
    const pendingCents = unpaidFines.filter(x=>s.pendingFines.indexOf(x.id) > -1).reduce((n,x)=>n+x.cents, 0);
    const defaultCard = s.cards.filter(c=>c.primary)[0] || s.cards[0] || null;

    const keyOn = s.keySaved && s.apiKey.length > 0;
    const country = COUNTRIES.filter(c=>c.name===s.reg.country)[0] || COUNTRIES[0];
    const userName = s.reg.name.trim() || 'Francisco Torreglosa';
    const userEmail = s.email.trim() || DEMO_EMAIL;

    return {
      t, logged:s.logged, snack:s.snack, closeSnack:()=>this.setState({snack:null}),
      isLogin: !s.logged && s.view==='login',
      isRegister: !s.logged && s.view==='register',
      isVerify: !s.logged && s.view==='verify',
      goLogin:()=>this.setState({view:'login'}),
      goRegister:()=>this.setState({view:'register'}),
      reg:s.reg, regEmail: s.reg.email || 'your inbox',
      countryOptions: COUNTRIES.map(c=>c.name),
      cityOptions: country.cities,
      onReg:{
        name:e=>setReg({name:e.target.value}),
        email:e=>setReg({email:e.target.value}),
        pass:e=>setReg({pass:e.target.value}),
        country:e=>{
          const c = COUNTRIES.filter(x=>x.name===e.target.value)[0] || COUNTRIES[0];
          setReg({country:c.name, city:c.cities[0]});
        },
        city:e=>setReg({city:e.target.value}),
        terms:()=>setReg({terms:!s.reg.terms})
      },
      termsBox: s.reg.terms ? 'check_box' : 'check_box_outline_blank',
      termsColor: s.reg.terms ? '#0C7F70' : '#5C7480',
      plans: PLANS.map(p=>({
        name:p.name, price:p.price, per:p.per, summary:p.summary, bullets:p.bullets,
        go:()=> s.logged
          ? (isStaff || p.name===s.role
              ? null
              : this.setState({planModal:{plan:p.name, step:'review', card:(s.cards.filter(c=>c.primary)[0]||s.cards[0]||{}).id}}))
          : setReg({plan:p.name}),
        note: s.logged && p.name===s.role ? 'Current plan'
          : (s.logged && s.planPending && s.planPending.plan===p.name && s.planPending.plan!==s.role ? 'Starts ' + CYCLE.renews
          : (s.logged && PLAN_RANK[p.name] > PLAN_RANK[s.role] ? 'Upgrade' : (s.logged ? 'Downgrade' : ''))),
        noteBg: s.logged && p.name===s.role ? 'rgba(12,127,112,.14)' : 'rgba(16,38,46,.06)',
        noteFg: s.logged && p.name===s.role ? '#0C7F70' : t.dim,
        hasNote: s.logged,
        border: (s.logged ? s.role : s.reg.plan)===p.name ? '#0C7F70' : (s.logged ? t.border : 'rgba(16,38,46,.16)'),
        bg: (s.logged ? s.role : s.reg.plan)===p.name ? 'rgba(12,127,112,.08)' : 'transparent',
        mark: (s.logged ? s.role : s.reg.plan)===p.name ? 'radio_button_checked' : 'radio_button_unchecked',
        markColor: (s.logged ? s.role : s.reg.plan)===p.name ? '#0C7F70' : t.dim
      })),
      planCycle: CYCLE,
      planStatus: (function(){
        if(!isMember) return null;
        const free = s.role==='Basic';
        const pend = s.planPending && s.planPending.plan !== s.role ? s.planPending : null;
        return {
          plan:s.role,
          line: free
            ? 'Basic is free, so there is nothing to renew.'
            : 'Active until ' + CYCLE.renews + ' · renews automatically at ' + (PLANS.filter(p=>p.name===s.role)[0]||{}).price,
          until: free ? 'No renewal' : CYCLE.renews,
          pending: pend ? pend.plan : '',
          hasPending: !!pend,
          pendingLine: pend
            ? 'You keep ' + s.role + ' until ' + CYCLE.renews + '. ' + pend.plan + ' starts that day and nothing is charged before then.'
            : '',
          cancelPending:()=>{
            this.setState({planPending:null});
            this.snack('Scheduled change cancelled. You stay on ' + s.role + '.', 'info');
          }
        };
      }).call(this),
      planModal: (function(){
        const m = s.planModal;
        if(!m) return null;
        const target = m.plan, cur = s.role;
        const up = PLAN_RANK[target] > PLAN_RANK[cur];
        const curC = PLAN_CENTS[cur], newC = PLAN_CENTS[target];
        const credit = Math.round(curC * CYCLE.left / CYCLE.days);
        const charge = Math.round(newC * CYCLE.left / CYCLE.days);
        const due = Math.max(charge - credit, 0);
        const card = s.cards.filter(c=>c.id===m.card)[0] || null;
        const set = patch => this.setState({planModal:Object.assign({}, m, patch)});
        return {
          isUp:up, isDown:!up, target, cur,
          kicker: up ? 'Upgrade' : 'Scheduled downgrade',
          title: up ? 'Move up to ' + target : 'Move down to ' + target,
          sub: up
            ? 'Your ' + cur + ' month runs to ' + CYCLE.renews + '. You only pay the difference for the ' + CYCLE.left + ' days left, never twice for the same period.'
            : 'Downgrades wait for the end of the period you already paid. Nothing is charged now and nothing is refunded.',
          rows: up
            ? [
                {k:target + ' for ' + CYCLE.left + ' remaining days', v:money(charge)},
                {k:'Credit for the ' + cur + ' days you already paid', v:'−' + money(credit)}
              ]
            : [
                {k:cur + ' stays active until', v:CYCLE.renews},
                {k:target + ' starts on', v:CYCLE.renews}
              ],
          dueLabel: up ? 'Due today' : 'Charged today',
          due: up ? money(due) : money(0),
          after: up
            ? 'From ' + CYCLE.renews + ' you pay ' + (PLANS.filter(p=>p.name===target)[0]||{}).price + ' every month.'
            : (PLAN_CENTS[target]
                ? 'From ' + CYCLE.renews + ' you pay ' + (PLANS.filter(p=>p.name===target)[0]||{}).price + ' every month.'
                : 'From ' + CYCLE.renews + ' you pay nothing. Borrowing narrows to your home library and the Basic catalog.'),
          losing: !up ? [
            cur==='Max' && target!=='Max' ? 'Reward points stop accruing and cannot be redeemed after the change.' : '',
            target==='Basic' ? 'Borrowing limited to ' + (s.reg.city ? s.reg.city + ' — your home library' : 'your home library') + ' and Basic-catalog titles.' : '',
            target==='Basic' ? 'AI recommendations turn off.' : ''
          ].filter(x=>x) : [],
          hasLosing: !up,
          needsCard: up && !card,
          cardLine: card ? card.brand + ' •••• ' + card.last4 + ' · exp ' + card.exp : 'No card saved yet',
          cardOpts: s.cards.map(c=>({
            id:c.id, label:c.brand + ' •••• ' + c.last4,
            go:()=>set({card:c.id}),
            bg: m.card===c.id ? 'rgba(12,127,112,.10)' : 'transparent',
            border: m.card===c.id ? '#0C7F70' : t.field,
            mark: m.card===c.id ? 'radio_button_checked' : 'radio_button_unchecked'
          })),
          addCard:()=>this.setState({planModal:null, cardModal:{data:Object.assign({}, EMPTY_CARD), confirming:false, dirty:false}}),
          isReview: m.step==='review', isConfirm: m.step==='confirm',
          cta: up ? 'Pay ' + money(due) + ' and upgrade' : 'Schedule the change',
          next:()=> up && !card
            ? this.snack('Add a payment method before upgrading.', 'error')
            : set({step:'confirm'}),
          back:()=>set({step:'review'}),
          confirmTitle: up ? 'Charge ' + money(due) + ' now?' : 'Schedule ' + target + ' for ' + CYCLE.renews + '?',
          confirmBody: up
            ? 'We charge ' + money(due) + ' to ' + (card ? card.brand + ' •••• ' + card.last4 : 'your card') + ' and ' + target + ' benefits apply immediately.'
            : 'You keep ' + cur + ' until ' + CYCLE.renews + '. On that date the plan becomes ' + target + '. You can cancel the change any time before then.',
          confirm:()=>this.run('plan', 1300, ()=>{
            if(up){
              const stamp = 'RC-2026081' + (5 + s.payments.length % 5);
              this.setState({
                role:target, planPending:null, planModal:null,
                payments:[{id:'pp'+(s.payments.length+1), date:'Aug 15, 2026',
                  desc:'Plan change — ' + cur + ' to ' + target + ' (prorated)',
                  method: card ? card.brand + ' •••• ' + card.last4 : 'Card', amount:money(due), receipt:stamp}].concat(s.payments)
              });
              this.snack('You are on ' + target + '. We charged ' + money(due) + ' for the rest of this cycle.', 'ok');
              this.notify('paid', 'Payment received — ' + money(due),
                'Prorated upgrade from ' + cur + ' to ' + target + '. Receipt ' + stamp + ' is in your payment history.', 'fines');
            } else {
              this.setState({planPending:{plan:target, on:CYCLE.renews}, planModal:null});
              this.snack(cur + ' stays active until ' + CYCLE.renews + '. ' + target + ' starts that day.', 'info');
            }
          }),
          busy: s.busy==='plan',
          close:()=>this.setState({planModal:null})
        };
      }).call(this),
      doRegister:()=>{
        const r = s.reg;
        if(!r.name.trim()) return this.snack('Enter your full name.', 'error');
        if(r.email.indexOf('@') < 1) return this.snack('That email address is not valid.', 'error');
        if(r.pass.length < 8) return this.snack('Password must be at least 8 characters.', 'error');
        if(!r.terms) return this.snack('You must accept the terms and data policy.', 'error');
        this.run('register', 1100, ()=>{
          this.setState({view:'verify'});
          this.snack('Account created. We sent an activation link to ' + r.email + '.', 'ok');
        });
      },
      activate:()=>this.run('activate', 1100, ()=>{
        this.setState({logged:true, view:'login', email:s.reg.email || DEMO_EMAIL, role:s.reg.plan});
        this.load('loans', 900); this.load('catalog', 1100); this.load('stats', 1000); this.load('recos', 1400);
        this.snack('Account activated. ' + s.reg.plan + ' plan active in ' + s.reg.city + '.', 'ok');
      }),
      resend:()=>this.run('resend', 900, ()=>this.snack('Activation link sent again. Check your spam folder too.', 'info')),
      planSummary: s.reg.plan + ' plan · ' + s.reg.city,
      email:s.email, pass:s.pass,
      onEmail:e=>this.setState({email:e.target.value}),
      onPass:e=>this.setState({pass:e.target.value}),
      submit:()=>this.run('login', 1000, ()=>{
        const acc = DEMO_ACCOUNTS[s.email.trim().toLowerCase()];
        if(acc && s.pass===DEMO_PASS){
          const staffRole = acc.role==='Admin' || acc.role==='Super Admin';
          this.setState({logged:true, snack:null, role:acc.role, route: staffRole ? 'admin-users' : 'home'});
          this.load('loans', 900); this.load('catalog', 1100); this.load('books', 900);
          this.load('stats', 1000); this.load('recos', 1400);
          this.snack(acc.hello, 'ok');
        } else {
          this.snack('Wrong username or password. Check your details and try again.', 'error');
        }
      }),
      busyLogin: busyOn('login'), busyRegister: busyOn('register'),
      busyActivate: busyOn('activate'), busyResend: busyOn('resend'),
      busyKey: busyOn('key'), busyFines: busyOn('fines'), busyAdd: busyOn('add'),
      notBusyLogin: !busyOn('login'), notBusyRegister: !busyOn('register'),
      notBusyActivate: !busyOn('activate'), notBusyResend: !busyOn('resend'),
      notBusyKey: !busyOn('key'), notBusyFines: !busyOn('fines'), notBusyAdd: !busyOn('add'),
      payFines:()=>this.setState({route:'fines'}),
      userName: (DEMO_ACCOUNTS[String(s.email).trim().toLowerCase()] || {}).name || userName,
      userEmail: userEmail,
      userRole: isMember ? (s.role + ' member · ' + s.reg.city) : (s.role + ' · ' + (isSuper ? 'all libraries' : scope.length + ' libraries')),
      memberSince: 'Member since 2021 · ' + s.reg.country,
      nav, pageTitle:titles[s.route] || 'Home',
      query:s.query, onQuery:e=>this.setState({query:e.target.value}),
      menuOpen:s.menuOpen, toggleMenu:()=>this.setState({menuOpen:!s.menuOpen}),
      toggleDark:()=>this.setState({dark:!s.dark}),
      themeIcon: s.dark ? 'light_mode' : 'dark_mode',
      userMenu:[
        {icon:'person', label:'Profile', go:go('profile'), fg:t.text},
        {icon:'settings', label:'Settings', go:go('settings'), fg:t.text},
        {icon:'help', label:'Help and support', go:go(isStaff ? 'admin-support' : 'support'), fg:t.text},
        {icon:'logout', label:'Sign out', fg:'#B3261E', go:()=>{this.setState({logged:false, menuOpen:false, email:'', pass:'', route:'home', view:'login'}); this.snack('Signed out.','info');}}
      ],
      isHome:s.route==='home', isCatalog:s.route==='catalog', isLoans:s.route==='loans',
      isAI:s.route==='ai', isSettings:s.route==='settings',
      isProfile:['profile','purchases'].indexOf(s.route)>-1,
      isFines: s.route==='fines',
      isAdmin: isStaff && s.route==='admin-books',
      isManual: isStaff && s.route==='admin-payments',
      isSupport: !isStaff && s.route==='support',
      isAdminSupport: isStaff && s.route==='admin-support',
      isUsers: isStaff && s.route==='admin-users',
      isSuperScreen: isSuper && s.route==='admin-libraries',
      isMember, isStaff, isSuper, role:s.role, scopeLabel: isSuper ? 'All libraries' : scope.join(' · '),
      staffRoleLabel: isSuper ? 'Super Admin — every library, can appoint admins' : 'Admin — full control of assigned libraries',
      roleSwitch: ROLES.map(r=>({
        label:r.key, note:r.note,
        go:()=>{
          const memberOnly = ['home','catalog','ai','loans','fines','purchases','profile'];
          const next = r.kind === 'member'
            ? (s.route==='admin-support' ? 'support'
               : (String(s.route).indexOf('admin')===0 || (r.key==='Basic' && s.route==='ai')) ? 'home' : s.route)
            : (s.route==='support' ? 'admin-support'
               : memberOnly.indexOf(s.route) > -1 ? 'admin-users' : s.route);
          this.setState({role:r.key, menuOpen:false, route:next, planPending:null});
          this.loadFor(next);
        },
        bg: s.role===r.key ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.role===r.key ? '#0E5A6E' : t.text,
        mark: s.role===r.key ? 'radio_button_checked' : 'radio_button_unchecked'
      })),
      statusFilters:['All','Draft','In catalog','In repair','Deleted'].map(x=>({
        label:x, go:()=>this.setState({adminFilter:x, page:Object.assign({}, s.page, {books:0})}),
        bg: s.adminFilter===x ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.adminFilter===x ? '#0E5A6E' : t.text,
        border: s.adminFilter===x ? '#0E5A6E' : t.field})),
      team: ADMIN_TEAM.map(a=>Object.assign({}, a, {pending:false}))
        .concat(s.invites.map(i=>Object.assign({}, i, {pending:true})))
        .map(a=>({
          name:a.name, email:a.email, role:a.role,
          since: a.pending ? 'Invited ' + a.sentAt : 'Since ' + a.since,
          libs: a.libs.length ? a.libs.join(' · ') : 'No libraries yet',
          count: a.pending ? 'Awaiting email confirmation' : a.libs.length + (a.libs.length===1?' library':' libraries'),
          pending:a.pending, active:!a.pending,
          chipBg: a.pending ? 'rgba(224,166,60,.20)' : a.role==='Super Admin' ? 'rgba(31,95,139,.16)' : 'rgba(16,168,140,.14)',
          chipFg: a.pending ? '#8A6A28' : a.role==='Super Admin' ? '#1F5F8B' : '#0C7F70',
          chipLabel: a.pending ? 'Invited' : a.role,
          statusIcon: a.pending ? 'mark_email_unread' : 'verified_user',
          busy: s.busy==='invite:'+a.email, idle: s.busy!=='invite:'+a.email,
          assign:()=>this.snack('Assign libraries to ' + a.name + ' — library picker opens here.', 'info'),
          elevate:()=>this.snack(a.role==='Super Admin' ? a.name + ' already has full powers.' : 'Granted extended powers to ' + a.name + ' on ' + a.libs.length + ' libraries.', a.role==='Super Admin' ? 'info' : 'ok'),
          resend:()=>this.run('invite:'+a.email, 1000, ()=>this.snack('Verification email sent again to ' + a.email + '.', 'ok')),
          revoke:()=>{ this.setState({invites:s.invites.filter(x=>x.email!==a.email)}); this.snack('Invitation for ' + a.email + ' was revoked.', 'info'); }
        })),
      userRows, userHeaders:this.headers('users', userCols), usersPager,
      userSearch:this.tableSearch('users','Filter by name, email, role or library'),
      usersEmpty: userRows.length===0 && !s.loading.users,
      usersLoading:s.loading.users, usersReady:!s.loading.users,
      refreshUsers:()=>this.load('users', 800),
      userCount: usersSorted.length + (usersSorted.length===1 ? ' user' : ' users'),
      userFilters:['All','Active','Pending verification','Blocked','Deleted'].map(x=>({
        label:x, go:()=>this.setState({userFilter:x, page:Object.assign({}, s.page, {users:0})}),
        bg: s.userFilter===x ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.userFilter===x ? '#0E5A6E' : t.text,
        border: s.userFilter===x ? '#0E5A6E' : t.field})),
      userDetail: (function(){
        if(!s.userDetail) return null;
        const u = userRows0.filter(x=>x.id===s.userDetail)[0];
        if(!u) return null;
        const act = s.userAction;
        const selfEmail = String(s.email || DEMO_EMAIL).toLowerCase();
        const isSelf = String(u.email).toLowerCase() === selfEmail;
        const targetSuper = u.role==='Super Admin';
        const targetStaff = targetSuper || u.role==='Admin';
        const canManage = !isSelf && (isSuper ? !targetSuper : !targetStaff);
        const lockReason = isSelf
          ? 'This is your own account. You cannot block or delete yourself.'
          : targetSuper
            ? 'Super admins cannot be blocked or deleted from this console. Another super admin must do it from the platform owner settings.'
            : (!isSuper && targetStaff)
              ? 'Only a super admin can block or delete another administrator.'
              : '';
        const start = kind => () => canManage
          ? this.setState({userAction:{kind:kind, id:u.id}})
          : this.snack(lockReason, 'error');
        const copy = {
          block:{title:'Block this user?', body:'“'+u.name+'” loses access immediately. Active reservations stay on record and fines remain payable.', cta:'Yes, block', accent:'#B3261E', done:'blocked', msg:' was blocked.'},
          unblock:{title:'Restore access?', body:'“'+u.name+'” can sign in and reserve again straight away.', cta:'Yes, restore', accent:'#0E5A6E', done:'active', msg:' is active again.'},
          remove:{title:'Delete this account?', body:'“'+u.name+'” is removed from the directory. Reservation history is kept for the audit log.', cta:'Yes, delete', accent:'#B3261E', done:'deleted', msg:' was deleted.'},
          restore:{title:'Restore this account?', body:'“'+u.name+'” returns to the directory as an active user.', cta:'Yes, restore', accent:'#0E5A6E', done:'active', msg:' was restored.'}
        }[act ? act.kind : 'block'];
        return {
          name:u.name, email:u.email, role:u.role, initials:u.name.split(' ').map(x=>x.charAt(0)).slice(0,2).join(''),
          statusLabel:u.statusLabel, statusBg:u.statusBg, statusFg:u.statusFg, statusIcon:u.statusIcon,
          rows:[
            {k:'Email', v:u.email}, {k:'Role', v:u.role},
            {k:'Home library', v:u.library}, {k:'City', v:u.city},
            {k:'Member since', v:u.joined}, {k:'Last activity', v:u.last}
          ],
          stats:[
            {k:'Active reservations', v:String(u.loans)},
            {k:'Outstanding fines', v:u.fines, warn:u.fines!=='$0.00'},
            {k:'Purchases', v:String(u.purchases)},
            {k:'On-time returns', v:u.onTime}
          ],
          canManage, locked:!canManage, lockReason,
          canResend: u.status==='pending' && !isSelf,
          isPending: u.status==='pending' && canManage, isBlocked: u.status==='blocked' && canManage,
          isDeleted: u.status==='deleted' && canManage, isActive: u.status==='active' && canManage,
          confirming: !!act, editing: !act,
          confirmTitle: copy.title, confirmBody: copy.body, confirmCta: copy.cta, accent: copy.accent,
          busy: s.busy==='user', idle: s.busy!=='user',
          block:start('block'), unblock:start('unblock'), remove:start('remove'), restore:start('restore'),
          cancelConfirm:()=>this.setState({userAction:null}),
          confirm:()=>this.run('user', 900, ()=>{
            this.setState({userStatus:Object.assign({}, s.userStatus, {[u.id]:copy.done}), userAction:null});
            this.snack('“'+u.name+'”' + copy.msg, copy.done==='active' ? 'ok' : 'info');
          }),
          resend:()=>this.run('user', 900, ()=>this.snack('Verification email sent again to ' + u.email + '.', 'ok')),
          selfNote: isSelf ? 'Signed in as this account' : '',
          close:()=>this.setState({userDetail:null, userAction:null})
        };
      }).call(this),
      isLibsCards: s.libsView==='cards', isLibsTable: s.libsView==='table',
      teamCount: (ADMIN_TEAM.length + s.invites.length) + ' administrators',
      libsViewModes:[
        {key:'cards', icon:'grid_view', label:'Card view'},
        {key:'table', icon:'table_rows', label:'Table view'}
      ].map(v=>({
        icon:v.icon, label:v.label, go:()=>this.setLibsView(v.key),
        bg: s.libsView===v.key ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.libsView===v.key ? '#0E5A6E' : t.dim,
        pressed: s.libsView===v.key ? 'true' : 'false'
      })),
      pendingCount: s.invites.length,
      hasPending: s.invites.length > 0,
      pendingNote: s.invites.length === 1
        ? '1 invitation is waiting for email confirmation.'
        : s.invites.length + ' invitations are waiting for email confirmation.',
      newAdmin:()=>this.setState({invite:{dirty:false, confirming:false,
        data:{name:'', email:'', role:'Admin', libs:[], note:''}}}),
      invite: (function(){
        const iv = s.invite;
        if(!iv) return null;
        const d = iv.data;
        const set = k => e => this.setState({invite:Object.assign({}, iv, {dirty:true, data:Object.assign({}, d, {[k]:e.target.value})})});
        const toggleLib = lib => () => {
          const has = d.libs.indexOf(lib) > -1;
          this.setState({invite:Object.assign({}, iv, {dirty:true,
            data:Object.assign({}, d, {libs: has ? d.libs.filter(x=>x!==lib) : d.libs.concat(lib)})})});
        };
        const isSuperRole = d.role==='Super Admin';
        const valid = !!(d.name.trim() && d.email.indexOf('@') > 0 && (isSuperRole || d.libs.length));
        return {
          name:d.name, email:d.email, role:d.role, note:d.note,
          onName:set('name'), onEmail:set('email'), onNote:set('note'),
          roles:['Admin','Super Admin'].map(r=>({label:r,
            note: r==='Admin' ? 'Full control of the libraries you assign' : 'Every library, can appoint other admins',
            go:()=>this.setState({invite:Object.assign({}, iv, {dirty:true, data:Object.assign({}, d, {role:r, libs: r==='Super Admin' ? LIBRARIES.slice() : d.libs})})}),
            bg: d.role===r ? 'rgba(12,127,112,.08)' : 'transparent',
            border: d.role===r ? '#0C7F70' : t.border,
            mark: d.role===r ? 'radio_button_checked' : 'radio_button_unchecked'})),
          libs: LIBRARIES.map(l=>({
            label:l, go:toggleLib(l),
            mark: d.libs.indexOf(l) > -1 ? 'check_box' : 'check_box_outline_blank',
            color: d.libs.indexOf(l) > -1 ? '#0C7F70' : t.dim,
            bg: d.libs.indexOf(l) > -1 ? 'rgba(12,127,112,.08)' : 'transparent',
            border: d.libs.indexOf(l) > -1 ? '#0C7F70' : t.border})),
          libSummary: isSuperRole ? 'Super admins reach every library automatically.'
            : d.libs.length ? d.libs.length + (d.libs.length===1?' library selected':' libraries selected') : 'Pick at least one library.',
          isSuperRole, notSuperRole: !isSuperRole,
          sendBg: valid ? '#0E5A6E' : t.field, sendCursor: valid ? 'pointer' : 'not-allowed',
          confirming:iv.confirming, editing:!iv.confirming,
          confirmBody: 'We will email ' + (d.email || 'this address') + ' a verification link. ' + (d.name || 'They') + ' becomes ' + d.role + ' only after confirming it, and shows as Invited here until then.',
          busy: s.busy==='invite', idle: s.busy!=='invite',
          submit:()=> valid
            ? this.setState({invite:Object.assign({}, iv, {confirming:true})})
            : this.snack(d.email.indexOf('@') < 1 ? 'Enter a valid work email.' : 'Name and at least one library are required.', 'error'),
          back:()=>this.setState({invite:Object.assign({}, iv, {confirming:false})}),
          confirm:()=>this.run('invite', 1300, ()=>{
            this.setState({invites:s.invites.concat({
              name:d.name, email:d.email.toLowerCase(), role:d.role,
              libs: isSuperRole ? LIBRARIES.slice() : d.libs, sentAt:'Aug 15, 2026'
            }), invite:null});
            this.snack('Invitation sent to ' + d.email + '. They appear as Invited until they confirm the email.', 'ok');
          }),
          requestClose:()=> iv.dirty ? this.setState({inviteExit:true}) : this.setState({invite:null})
        };
      }).call(this),
      inviteExit: s.inviteExit ? {
        stay:()=>this.setState({inviteExit:false}),
        leave:()=>{ this.setState({invite:null, inviteExit:false}); this.snack('Invitation discarded. No email was sent.', 'info'); }
      } : null,
      goSettings:go('settings'), goLoans:go('loans'), goAI:go('ai'),
      stats:[
        {label:'Reserved', value:'4', note:'2 due this week', icon:'bookmarks', color:'#0C7F70'},
        {label:'Fines', value: money(dueCents), note: openFines.length ? openFines.length + ' overdue title' + (openFines.length===1?'':'s') : 'Nothing outstanding', icon:'warning', color: dueCents ? '#B3261E' : '#0F7A63'},
        {label:'Purchased', value:'11', note:'Last: Aug 3, 2026', icon:'shopping_bag', color:'#0F7A63'},
        {label:'Read in 2026', value:'27', note:'+6 vs. 2025', icon:'trending_up', color:'#0E5A6E'}
      ],
      loans, dashLoans:loansSorted.slice(0,4),
      loanHeaders:this.headers('loans', resCols),
      dashLoanHeaders:this.headers('loans', [resCols[0], resCols[2], resCols[4], resCols[5]]),
      loansPager, loanSearch:this.tableSearch('loans','Filter reservations by book, author or status'),
      loansEmpty: loans.length===0 && !s.loading.loans,
      skel:[1,2,3,4,5], skel3:[1,2,3], skel4:[1,2,3,4], skelBg:t.skel,
      supportNext: 'TCK-' + (2038 + s.tickets.length),
      openNewTicket: ()=>this.setState({ticketNew:{cat:TICKET_CATS[0], subject:'', body:''}}),
      ticketNew: (function(){
        const d = s.ticketNew;
        if(!d) return null;
        const set = k => e => this.setState({ticketNew:Object.assign({}, d, {[k]:e.target.value})});
        const ready = d.subject.trim().length > 4 && d.body.trim().length > 14;
        return {
          cat:d.cat, subject:d.subject, body:d.body,
          cats:TICKET_CATS.map(c=>({label:c, value:c})),
          onCat:set('cat'), onSubject:set('subject'), onBody:set('body'),
          count: d.body.length + ' / 800',
          ready:ready, ctaBg: ready ? '#0E5A6E' : t.field, ctaFg: ready ? '#fff' : t.dim,
          ctaCursor: ready ? 'pointer' : 'not-allowed',
          busy:s.busy==='ticket', idle:s.busy!=='ticket',
          close:()=>this.setState({ticketNew:null}),
          submit:()=> !ready
            ? this.snack('Add a subject and a bit more detail so an agent can help.', 'error')
            : this.run('ticket', 1100, ()=>{
                const id = 'TCK-' + (2038 + s.tickets.length);
                const rec = {id:id, user:userName, email:userEmail, self:true,
                  subject:d.subject.trim(), category:d.cat, library:HOME_LIBRARY,
                  status:'created', owner:'', created:'Aug 15, 2026', updated:'Aug 15, 2026',
                  rating:0, review:'',
                  msgs:[{who:'member', name:userName, time:'Aug 15, 2026 · now', text:d.body.trim()}]};
                this.setState({tickets:[rec].concat(s.tickets), ticketNew:null, ticketOpen:id});
                this.snack('Ticket ' + id + ' created. An agent answers within one business day.', 'ok');
                this.notify('support', 'New ticket ' + id + ' from ' + userName,
                  d.cat + ' · “' + d.subject.trim() + '”. Nobody is assigned yet.', 'admin-support');
              })
        };
      }).call(this),
      ticketFilters: ['All', 'Created', 'In review', 'Resolved'].map(x=>({
        label:x, bg: s.ticketFilter===x ? 'rgba(14,90,110,.12)' : 'transparent',
        fg: s.ticketFilter===x ? P : t.text,
        border: s.ticketFilter===x ? P : t.field,
        go:()=>this.setState({ticketFilter:x})
      })),
      ticketSearch:this.tableSearch('tickets','Filter by number, member or subject'),
      isTicketCards: s.supportView==='cards', isTicketTable: s.supportView==='table',
      supportModes:[
        {key:'cards', icon:'view_agenda', label:'Card view'},
        {key:'table', icon:'table_rows', label:'Table view'}
      ].map(v=>({
        icon:v.icon, label:v.label, go:()=>this.setSupportView(v.key),
        bg: s.supportView===v.key ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.supportView===v.key ? '#0E5A6E' : t.dim,
        pressed: s.supportView===v.key ? 'true' : 'false'
      })),
      ticketsLoading:s.loading.tickets, ticketsReady:!s.loading.tickets,
      refreshTickets:()=>this.load('tickets', 800),
      ticketRows: (function(){
        const qq = (s.tq.tickets||'').trim().toLowerCase();
        return s.tickets
          .filter(x => isStaff ? (isSuper || scope.indexOf(x.library) > -1) : x.self)
          .filter(x => s.ticketFilter==='All' || TICKET_STATUS[x.status][0]===s.ticketFilter)
          .filter(x => !qq || (x.id + ' ' + x.user + ' ' + x.subject + ' ' + x.category).toLowerCase().indexOf(qq) > -1)
          .map(x => {
            const st = TICKET_STATUS[x.status];
            const last = x.msgs[x.msgs.length - 1];
            return {
              id:x.id, subject:x.subject, category:x.category, user:x.user, email:x.email,
              created:x.created, updated:x.updated, library:x.library,
              owner: x.owner || 'Unassigned', hasOwner: !!x.owner, unassigned: !x.owner,
              statusLabel:st[0], statusBg:st[1], statusFg:st[2], statusIcon:st[3],
              lastWho: last.who==='agent' ? 'Support' : last.name.split(' ')[0],
              lastText: last.text.length > 120 ? last.text.slice(0, 120) + '…' : last.text,
              msgCount: x.msgs.length + (x.msgs.length===1 ? ' message' : ' messages'),
              rated: x.rating > 0, ratingStars: '★★★★★'.slice(0, x.rating) + '☆☆☆☆☆'.slice(0, 5 - x.rating),
              needsRating: x.status==='resolved' && !x.rating && !isStaff,
              open:()=>this.setState({ticketOpen:x.id, ticketReply:'', ticketStars:x.rating, ticketReview:x.review})
            };
          });
      }).call(this),
      ticketsEmpty: (function(){
        const qq = (s.tq.tickets||'').trim().toLowerCase();
        const n = s.tickets
          .filter(x => isStaff ? (isSuper || scope.indexOf(x.library) > -1) : x.self)
          .filter(x => s.ticketFilter==='All' || TICKET_STATUS[x.status][0]===s.ticketFilter)
          .filter(x => !qq || (x.id + ' ' + x.user + ' ' + x.subject + ' ' + x.category).toLowerCase().indexOf(qq) > -1).length;
        return n === 0 && !s.loading.tickets;
      }).call(this),
      ticketsEmptyCards: (function(){
        const qq = (s.tq.tickets||'').trim().toLowerCase();
        const n = s.tickets
          .filter(x => isStaff ? (isSuper || scope.indexOf(x.library) > -1) : x.self)
          .filter(x => s.ticketFilter==='All' || TICKET_STATUS[x.status][0]===s.ticketFilter)
          .filter(x => !qq || (x.id + ' ' + x.user + ' ' + x.subject + ' ' + x.category).toLowerCase().indexOf(qq) > -1).length;
        return n === 0 && !s.loading.tickets && s.supportView==='cards';
      }).call(this),
      supportStats: (function(){
        const mine = s.tickets.filter(x => isSuper || scope.indexOf(x.library) > -1);
        const rated = mine.filter(x=>x.rating > 0);
        const avg = rated.length ? (rated.reduce((a,b)=>a+b.rating, 0) / rated.length).toFixed(1) : '—';
        return [
          {k:'Waiting for an owner', v:String(mine.filter(x=>x.status==='created').length), icon:'fiber_new', fg:'#8A6A28'},
          {k:'In review', v:String(mine.filter(x=>x.status==='review').length), icon:'hourglass_top', fg:'#0E5A6E'},
          {k:'Resolved', v:String(mine.filter(x=>x.status==='resolved').length), icon:'task_alt', fg:'#0F7A63'},
          {k:'Service rating', v:avg + (rated.length ? ' / 5' : ''), icon:'star', fg:'#E0A63C'}
        ];
      }).call(this),
      supportHelp: [
        {q:'How do fines work?', a:'A late return costs $0.35 a day per title. Pay by card in Fines & payments or get a code and pay at the desk.'},
        {q:'Can I change a courier pickup?', a:'Yes, until the courier scans the parcel. Open the reservation and start a new pickup.'},
        {q:'When does an upgrade apply?', a:'Immediately. You only pay the prorated difference for the days left in the cycle.'}
      ],
      ticketDetail: (function(){
        if(!s.ticketOpen) return null;
        const x = s.tickets.filter(y=>y.id===s.ticketOpen)[0];
        if(!x) return null;
        const st = TICKET_STATUS[x.status];
        const patch = (upd, extra) => {
          this.setState(Object.assign({tickets:s.tickets.map(y=>y.id===x.id?Object.assign({}, y, upd):y)}, extra||{}));
        };
        const canReply = x.status !== 'resolved' || isStaff;
        return {
          id:x.id, subject:x.subject, category:x.category, created:x.created, updated:x.updated,
          user:x.user, email:x.email, library:x.library,
          owner: x.owner || 'Unassigned', hasOwner: !!x.owner,
          statusLabel:st[0], statusBg:st[1], statusFg:st[2], statusIcon:st[3],
          isStaffView:isStaff, isMemberView:!isStaff,
          resolved: x.status==='resolved', live: x.status !== 'resolved',
          meta: isStaff
            ? [{k:'Member', v:x.user}, {k:'Email', v:x.email}, {k:'Library', v:x.library},
               {k:'Category', v:x.category}, {k:'Created', v:x.created}, {k:'Owner', v:x.owner || 'Unassigned'}]
            : [{k:'Category', v:x.category}, {k:'Created', v:x.created},
               {k:'Last update', v:x.updated}, {k:'Handled by', v:x.owner || 'Waiting for an agent'}],
          msgs: x.msgs.map(m=>({
            name: m.who==='agent' ? m.name + ' · Support' : m.name,
            time:m.time, text:m.text,
            bg: m.who==='agent' ? (s.dark ? 'rgba(14,90,110,.22)' : 'rgba(14,90,110,.08)') : (s.dark ? 'rgba(255,255,255,.05)' : 'rgba(16,38,46,.04)'),
            align: m.who==='agent' ? 'flex-start' : 'flex-end',
            initials: m.name.split(' ').map(w=>w.charAt(0)).slice(0,2).join('')
          })),
          canReply:canReply,
          replyLabel: isStaff ? 'Reply to ' + x.user.split(' ')[0] : 'Add a message',
          reply:s.ticketReply,
          onReply:e=>this.setState({ticketReply:e.target.value}),
          busy:s.busy==='reply', idle:s.busy!=='reply',
          send:()=> s.ticketReply.trim().length < 2
            ? this.snack('Write a message first.', 'error')
            : this.run('reply', 900, ()=>{
                const msg = {who: isStaff ? 'agent' : 'member', name:userName, time:'Aug 15, 2026 · now', text:s.ticketReply.trim()};
                patch({msgs:x.msgs.concat([msg]), updated:'Aug 15, 2026',
                  status: isStaff && x.status==='created' ? 'review' : x.status,
                  owner: isStaff && !x.owner ? userName : x.owner}, {ticketReply:''});
                this.snack('Reply sent on ' + x.id + '.', 'ok');
                this.notify('support',
                  isStaff ? 'Support replied to ' + x.id : x.user.split(' ')[0] + ' replied on ' + x.id,
                  isStaff ? '“' + x.subject + '” is in review with ' + userName + '.' : '“' + x.subject + '” is waiting for your answer.',
                  isStaff ? 'support' : 'admin-support');
              }),
          canAssign: isStaff && !x.owner,
          assign:()=>this.run('reply', 700, ()=>{
            patch({owner:userName, status: x.status==='created' ? 'review' : x.status, updated:'Aug 15, 2026'});
            this.snack(x.id + ' assigned to you.', 'ok');
            this.notify('support', x.id + ' is in review', userName + ' is looking at “' + x.subject + '”.', 'support');
          }),
          canResolve: isStaff && x.status !== 'resolved',
          resolve:()=>this.run('reply', 900, ()=>{
            patch({status:'resolved', owner:x.owner || userName, updated:'Aug 15, 2026'});
            this.snack(x.id + ' marked as resolved.', 'ok');
            this.notify('support', x.id + ' was resolved',
              '“' + x.subject + '” is closed. Rate the service in Help & support.', 'support');
          }),
          canReopen: !isStaff && x.status==='resolved',
          reopen:()=>this.run('reply', 800, ()=>{
            patch({status:'review', updated:'Aug 15, 2026',
              msgs:x.msgs.concat([{who:'member', name:userName, time:'Aug 15, 2026 · now', text:'Reopening: this is still happening.'}])});
            this.snack(x.id + ' reopened. The same agent picks it up.', 'ok');
            this.notify('support', x.id + ' was reopened', x.user + ' says the problem is still happening.', 'admin-support');
          }),
          showRating: !isStaff && x.status==='resolved' && !x.rating,
          hasRating: x.rating > 0,
          ratingValue: x.rating ? x.rating + ' / 5' : '',
          ratingReview: x.review,
          ratingBy: isStaff ? x.user + ' rated this ticket' : 'You rated this ticket',
          ratedStars: [1,2,3,4,5].map(n=>({icon: n <= x.rating ? 'star' : 'star_border'})),
          stars: [1,2,3,4,5].map(n=>({
            icon: n <= s.ticketStars ? 'star' : 'star_border',
            color: n <= s.ticketStars ? '#E0A63C' : t.dim,
            go:()=>this.setState({ticketStars:n})
          })),
          starNote: ['Pick a score', 'Poor service', 'Below expectations', 'Fine', 'Good', 'Excellent'][s.ticketStars],
          reviewText:s.ticketReview,
          onReview:e=>this.setState({ticketReview:e.target.value}),
          rateReady: s.ticketStars > 0,
          rateBg: s.ticketStars > 0 ? '#0E5A6E' : t.field,
          rateFg: s.ticketStars > 0 ? '#fff' : t.dim,
          rateCursor: s.ticketStars > 0 ? 'pointer' : 'not-allowed',
          rate:()=> !s.ticketStars
            ? this.snack('Pick a score from 1 to 5 first.', 'error')
            : this.run('reply', 900, ()=>{
                patch({rating:s.ticketStars, review:s.ticketReview.trim()});
                this.snack('Thanks — your rating helps us staff the desk.', 'ok');
                this.notify('support', x.id + ' rated ' + s.ticketStars + '/5',
                  (x.owner || 'The desk') + ' handled “' + x.subject + '”.', 'admin-support');
              }),
          close:()=>this.setState({ticketOpen:null, ticketReply:''})
        };
      }).call(this),
      bellIcon: s.notifOn ? 'notifications' : 'notifications_off',
      notifChip: s.notifOn ? (s.notifSound ? 'On, with sound' : 'On, silent') : 'Off',
      notifChipBg: s.notifOn ? 'rgba(15,122,99,.12)' : 'rgba(16,38,46,.08)',
      notifChipFg: s.notifOn ? '#0F7A63' : t.dim,
      notesOpen: s.notesOpen,
      toggleNotes: ()=>this.setState({notesOpen:!s.notesOpen, menuOpen:false}),
      closeNotes: ()=>this.setState({notesOpen:false}),
      unread: s.notes.filter(n=>!n.read).length,
      hasUnread: s.notes.filter(n=>!n.read).length > 0,
      unreadBadge: (function(n){ return n > 99 ? '+99' : String(n); })(s.notes.filter(n=>!n.read).length),
      unreadLine: (function(n){
        return n === 0 ? 'You are all caught up' : n === 1 ? '1 unread notification' : n + ' unread notifications';
      })(s.notes.filter(n=>!n.read).length),
      notifOn: s.notifOn, notifOff: !s.notifOn,
      noteRows: s.notes.slice(0, 40).map(n => {
        const d = NOTE_KINDS[n.kind] || NOTE_KINDS.paid;
        return {
          id:n.id, title:n.title, body:n.body, time:n.time,
          icon:d.icon, iconFg:d.fg, iconBg:d.bg,
          unread:!n.read,
          rowBg: n.read ? 'transparent' : (s.dark ? 'rgba(14,90,110,.14)' : 'rgba(14,90,110,.05)'),
          open:()=>{
            this.setState({notes:s.notes.map(x=>x.id===n.id?Object.assign({},x,{read:true}):x),
              notesOpen:false, route:n.route});
            this.loadFor(n.route);
          }
        };
      }),
      notesEmpty: s.notes.length === 0,
      notesAny: s.notes.length > 0,
      markAllRead: ()=>{
        if(!s.notes.filter(n=>!n.read).length) return this.snack('Nothing left to read.', 'info');
        this.setState({notes:s.notes.map(n=>Object.assign({}, n, {read:true}))});
        this.snack('All notifications marked as read.', 'ok');
      },
      clearNotes: ()=>{ this.setState({notes:[], notesOpen:false}); this.snack('Notification list cleared.', 'ok'); },
      openNotifSettings: ()=>{ this.setState({notesOpen:false, route:'settings'}); this.loadFor('settings'); },
      notifSwitches: [
        {key:'master', label:'Notifications', note:'Turn the whole notification centre on or off', on:s.notifOn},
        {key:'sound', label:'Notification sound', note:'A short chime when something arrives', on:s.notifOn && s.notifSound, off:!s.notifOn},
        {key:'due', label:'Due dates and overdue fines', note:'Reminders 48 h ahead and every overdue day', on:s.notifOn && s.notifKinds.due, off:!s.notifOn},
        {key:'payments', label:'Payments', note:'Codes waiting at the desk, charges and validations', on:s.notifOn && s.notifKinds.payments, off:!s.notifOn},
        {key:'returns', label:'Returns', note:'Courier pickups and check-ins confirmed by the library', on:s.notifOn && s.notifKinds.returns, off:!s.notifOn},
        {key:'holds', label:'Reservations and holds', note:'When a reservation is confirmed or a copy frees up', on:s.notifOn && s.notifKinds.holds, off:!s.notifOn},
        {key:'support', label:'Support tickets', note:'Replies, status changes and new tickets to answer', on:s.notifOn && s.notifKinds.support, off:!s.notifOn}
      ].map(x=>({
        label:x.label, note:x.note, on:x.on,
        opacity: x.off ? '.45' : '1',
        track: x.on ? P : (s.dark ? 'rgba(255,255,255,.24)' : 'rgba(16,38,46,.24)'),
        justify: x.on ? 'flex-end' : 'flex-start',
        cursor: x.off ? 'not-allowed' : 'pointer',
        go:()=>{
          if(x.key==='master'){
            const on = !s.notifOn;
            this.setState({notifOn:on, notesOpen:false});
            return this.snack(on ? 'Notifications are on.' : 'Notifications are off. Nothing new will reach you.', on ? 'ok' : 'info');
          }
          if(!s.notifOn) return this.snack('Turn notifications on first.', 'info');
          if(x.key==='sound'){
            const on = !s.notifSound;
            this.setState({notifSound:on});
            if(on) setTimeout(()=>this.ding(), 60);
            return this.snack(on ? 'Sound on.' : 'Sound off. The badge still counts.', 'ok');
          }
          this.setState({notifKinds:Object.assign({}, s.notifKinds, {[x.key]:!s.notifKinds[x.key]})});
        }
      })),
      roleOpts:[
        {label:'Member', note:'returns by courier only', key:'member'},
        {label:'Librarian', note:'receives the physical copy', key:'librarian'}
      ].map(r=>({label:r.label, note:r.note,
        go:()=>this.setState({deskRole:r.key}),
        bg: s.deskRole===r.key ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.deskRole===r.key ? '#0E5A6E' : t.dim,
        border: s.deskRole===r.key ? '#0E5A6E' : t.field})),
      showDeskRoles: isStaff,
      roleNote: isLibrarian
        ? 'Librarian view: check in a copy once you physically hold it.'
        : 'Member view: hand the copy to the courier and enter their pickup code.',
      courier: (function(){
        if(!s.codeFor) return null;
        const l = s.added.concat(loanData).filter(x=>x.id===s.codeFor)[0];
        if(!l) return null;
        const expected = this.pickupCode(l.id);
        return {
          title:l.title, author:l.author, due:l.due, expected,
          kicker: s.prefs.ret==='branch' ? 'Library drop-off' : 'Courier pickup',
          intro: s.prefs.ret==='branch'
            ? 'Hand the copy to the desk, then type the code the librarian reads out. The reservation moves to '
            : 'Hand the book to the courier, then type the code they read out. The reservation moves to ',
          codeLabel: s.prefs.ret==='branch' ? 'Drop-off code' : 'Pickup code',
          demoWho: s.prefs.ret==='branch' ? 'the librarian at the desk is' : 'the courier assigned to this pickup is',
          confirmLabel: s.prefs.ret==='branch' ? 'Confirm drop-off' : 'Confirm pickup',
          value:s.codeInput, busy:s.busy==='code', idle:s.busy!=='code',
          copyExpected:this.copier(expected, s.prefs.ret==='branch' ? 'Drop-off code' : 'Pickup code'),
          onInput:e=>this.setState({codeInput:e.target.value}),
          close:()=>this.setState({codeFor:null, codeInput:''}),
          confirm:()=>{
            if(s.codeInput.trim().toUpperCase() !== expected)
              return this.snack(s.prefs.ret==='branch'
                ? 'That drop-off code is not valid. Ask the librarian to read it again.'
                : 'That pickup code is not valid. Ask the courier to read it again.', 'error');
            this.run('code', 1000, ()=>{
              this.setState({inTransit:s.inTransit.concat(l.id), codeFor:null, codeInput:''});
              this.snack((s.prefs.ret==='branch' ? 'Drop-off' : 'Pickup') + ' confirmed. “'+l.title+'” is now Return in progress.', 'ok');
              this.notify('transit', '“'+l.title+'” is on its way back',
                (s.prefs.ret==='branch' ? 'The desk took the copy' : 'The courier picked the copy up') + ' and the reservation is Return in progress. We confirm again when the library checks it in.', 'loans');
            });
          }
        };
      }).call(this),
      buy: (function(){
        if(!s.buyModal) return null;
        const bm = s.buyModal;
        const b = shown.filter(x=>x.id===bm.id)[0];
        if(!b) return null;
        const ship = bm.fulfil==='ship';
        const price = Number(String(b.price).replace(/[^0-9.]/g,'')) || 0;
        const discount = plan==='Max' ? 0.15 : plan==='Plus' ? 0.10 : 0;
        const off = price * discount;
        const fee = ship ? DELIVERY_FEE : 0;
        const total = price - off + fee;
        const card = s.cards.filter(c=>c.id===bm.method)[0] || s.cards.filter(c=>c.primary)[0] || s.cards[0];
        const set = k => v => this.setState({buyModal:Object.assign({}, bm, {[k]:v})});
        return {
          title:b.title, author:b.author, tint:b.tint, cover:b.cover, hasCover:!!b.cover, coverBg: b.cover ? 'url("' + b.cover + '")' : 'none', genre:b.genre,
          defaultNote: 'Your default: ' + (s.prefs.purchase==='ship' ? 'ship to my address' : 'collect at library'),
          pickMark: ship ? 'radio_button_unchecked' : 'radio_button_checked',
          shipMark: ship ? 'radio_button_checked' : 'radio_button_unchecked',
          pickBg: ship ? 'transparent' : 'rgba(14,90,110,.10)', pickBorder: ship ? t.border : P,
          shipBg: ship ? 'rgba(14,90,110,.10)' : 'transparent', shipBorder: ship ? P : t.border,
          pickNote: 'Ready in 2 h at ' + b.branch,
          setPick: set('fulfil').bind(null,'pickup'), setShip: set('fulfil').bind(null,'ship'),
          price: '$' + price.toFixed(2),
          hasDiscount: discount > 0, discountLabel: plan + ' discount · ' + Math.round(discount*100) + '%',
          discount: '-$' + off.toFixed(2),
          feeLabel: ship ? '$' + fee.toFixed(2) : 'Free',
          total: '$' + total.toFixed(2),
          methods: s.cards.map(c=>({
            label: c.brand + ' •••• ' + c.last4, note: 'Expires ' + c.exp,
            mark: (card && c.id===card.id) ? 'radio_button_checked' : 'radio_button_unchecked',
            bg: (card && c.id===card.id) ? 'rgba(14,90,110,.10)' : 'transparent',
            border: (card && c.id===card.id) ? P : t.border,
            go: set('method').bind(null, c.id)
          })),
          hasCard: !!card, noCard: !card,
          addCard:()=>this.setState({buyModal:null, route:'settings', cardModal:{data:Object.assign({}, EMPTY_CARD), confirming:false, dirty:false}}),
          confirmBg: card ? '#0E5A6E' : t.field, confirmCursor: card ? 'pointer' : 'not-allowed',
          busy: s.busy==='buy', idle: s.busy!=='buy',
          close:()=>this.setState({buyModal:null}),
          confirm:()=>{
            if(!card) return this.snack('Add a payment method before buying.', 'error');
            this.run('buy', 1300, ()=>{
              const rec = {id:'p'+Date.now(), date:'Aug 15, 2026',
                desc:'Purchase — ' + b.title, method: card.brand + ' •••• ' + card.last4,
                amount:'$' + total.toFixed(2), receipt:'RC-2026081' + (s.payments.length + 1)};
              this.setState({payments:[rec].concat(s.payments), buyModal:null, route:'purchases'});
              this.load('ledger', 700);
              this.snack('“'+b.title+'” purchased for $' + total.toFixed(2) + ' · ' +
                (ship ? 'shipping in 3–5 days.' : 'collect at ' + b.branch + ' in 2 h.'), 'ok');
            });
          }
        };
      }).call(this),
      loansLoading:s.loading.loans, loansReady:!s.loading.loans,
      ledgerLoading:s.loading.ledger, ledgerReady:!s.loading.ledger,
      booksLoading:s.loading.books, booksReady:!s.loading.books,
      catalogLoading:s.loading.catalog, catalogReady:!s.loading.catalog,
      statsLoading:s.loading.stats, statsReady:!s.loading.stats,
      recosLoading:s.loading.recos, recosReady:!s.loading.recos,
      refreshRecos:()=>this.load('recos', 1000),
      refreshLoans:()=>this.load('loans', 800),
      refreshLedger:()=>this.load('ledger', 800),
      refreshBooks:()=>this.load('books', 800),
      navOpen:s.navOpen,
      toggleNav:()=>this.setState({navOpen:!s.navOpen}),
      navIcon: s.navOpen ? 'menu_open' : 'menu',
      shellCols: (s.navOpen ? '264px' : '78px') + ' minmax(0,1fr)',
      navLabel: s.navOpen ? 'block' : 'none',
      navFlexLabel: s.navOpen ? 'flex' : 'none',
      navJustify: s.navOpen ? 'flex-start' : 'center',
      navItemPad: s.navOpen ? '0 12px' : '0',
      recos:recoList.slice(0,3), recosFull:recoList,
      fabOpen:s.fabOpen, fabClosed:!s.fabOpen,
      fabIcon: s.fabOpen ? 'close' : 'bolt',
      fabVisible:!s.fabDocked, fabDocked:s.fabDocked,
      toggleFab:()=>this.setState({fabOpen:!s.fabOpen}),
      hideFab:()=>this.setState({fabDocked:true, fabOpen:false}),
      showFab:()=>this.setState({fabDocked:false, fabOpen:true}),
      quick: isStaff ? [
        {icon:'group', label:'Users', go:()=>{ this.setState({route:'admin-users', fabOpen:false}); this.loadFor('admin-users'); }},
        {icon:'library_add', label:'Book management', go:()=>{ this.setState({route:'admin-books', fabOpen:false}); this.loadFor('admin-books'); }},
        {icon:'settings', label:'AI settings', go:()=>this.setState({route:'settings', fabOpen:false})}
      ] : [
        {icon:'qr_code_scanner', label:'Quick check-in', go:()=>this.setState({route:'loans', fabOpen:false})},
        {icon:'search', label:'Search catalog', go:()=>this.setState({route:'catalog', fabOpen:false})},
        {icon:'local_shipping', label:'Delivery status', go:()=>this.setState({route:'loans', fabOpen:false})},
        {icon:'payments', label:'Pay fines', go:()=>this.setState({route:'fines', fabOpen:false})}
      ],
      catalogRows, catalogHeaders:this.headers('catalog', catalogCols), catalogPager,
      isGridView: s.catalogView==='grid', isTableView: s.catalogView==='table',
      viewModes:[
        {key:'grid', icon:'grid_view', label:'Card view'},
        {key:'table', icon:'table_rows', label:'Table view'}
      ].map(v=>({
        icon:v.icon, label:v.label, go:()=>this.setCatalogView(v.key),
        bg: s.catalogView===v.key ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.catalogView===v.key ? '#0E5A6E' : t.dim,
        pressed: s.catalogView===v.key ? 'true' : 'false'
      })),
      shown, filters, resultCount: shown.length + (shown.length===1?' title':' titles'),
      bookRows, bookHeaders:this.headers('books', bookCols), booksPager,
      bookSearch:this.tableSearch('books','Filter by title, author, ISBN or genre'),
      booksEmpty: bookRows.length===0 && !s.loading.books,
      ledgerRows, ledgerHeaders:this.headers('ledger', ledgerCols), ledgerPager,
      ledgerSearch:this.tableSearch('ledger','Filter entries'),
      ledgerEmpty: ledgerRows.length===0 && !s.loading.ledger,
      fineTotal: money(dueCents),
      hasFines: dueCents > 0, noFines: dueCents === 0,
      finesList: FINES_SEED.map(x=>{
        const paid = s.paidFines.indexOf(x.id) > -1;
        const pend = !paid && s.pendingFines.indexOf(x.id) > -1;
        return {title:x.title, reason:x.reason, date:x.date, amount:money(x.cents), paid:paid, open:!paid,
          chipBg: paid ? 'rgba(16,168,140,.14)' : pend ? 'rgba(224,166,60,.20)' : 'rgba(179,38,30,.12)',
          chipFg: paid ? '#0C7F70' : pend ? '#8A6A28' : '#B3261E',
          chipLabel: paid ? 'Paid' : pend ? 'Awaiting validation' : 'Outstanding'};
      }),
      cardList: s.cards.map(c=>({
        brand:c.brand, last4:c.last4, exp:c.exp, holder:c.holder,
        primary:c.primary, secondary:!c.primary,
        icon: c.brand==='Amex' ? 'credit_card' : 'credit_card',
        makeDefault:()=>{ this.setState({cards:s.cards.map(x=>Object.assign({}, x, {primary:x.id===c.id}))});
          this.snack(c.brand + ' •••• ' + c.last4 + ' is now your default method.', 'ok'); },
        remove:()=>this.setState({cardRemove:c.id})
      })),
      payments: s.payments.map(p=>Object.assign({}, p, {copyReceipt:this.copier(p.receipt, 'Receipt')})),
      addCard:()=>this.setState({cardModal:{data:Object.assign({}, EMPTY_CARD), confirming:false, dirty:false}}),
      openPay:()=> dueCents === 0
        ? this.snack('You have no outstanding fines. Nothing to pay.', 'info')
        : this.setState({payModal:{step:'select', picked:openFines.map(x=>x.id), method:(defaultCard||{}).id, receipt:''}}),
      payModal: (function(){
        const p = s.payModal;
        if(!p) return null;
        const picked = openFines.filter(x=>p.picked.indexOf(x.id) > -1);
        const total = picked.reduce((n,x)=>n+x.cents, 0);
        const isManual = p.method === 'manual';
        const method = isManual ? null : (s.cards.filter(c=>c.id===p.method)[0] || defaultCard);
        const chosen = isManual || !!method;
        return {
          isManual: isManual, isCard: !isManual,
          confirmTitle: isManual ? 'Generate a payment code?' : 'Confirm this payment?',
          confirmCta: isManual ? 'Generate code' : 'Yes, pay now',
          confirmNote: isManual
            ? 'Nothing is charged now. Take the code to the desk within 72 hours; a librarian validates the cash or card payment and your fines clear.'
            : '',
          doneTitle: isManual ? 'Payment code ready' : 'Payment complete',
          doneIcon: isManual ? 'confirmation_number' : 'check_circle',
          codeLabel: isManual ? 'Payment code' : 'Receipt',
          isSelect:p.step==='select', isConfirm:p.step==='confirm', isDone:p.step==='done',
          items: openFines.map(x=>({
            title:x.title, reason:x.reason, date:x.date, amount:money(x.cents),
            mark: p.picked.indexOf(x.id) > -1 ? 'check_box' : 'check_box_outline_blank',
            color: p.picked.indexOf(x.id) > -1 ? '#0C7F70' : t.dim,
            border: p.picked.indexOf(x.id) > -1 ? '#0C7F70' : t.border,
            bg: p.picked.indexOf(x.id) > -1 ? 'rgba(12,127,112,.06)' : 'transparent',
            go:()=>this.setState({payModal:Object.assign({}, p, {picked: p.picked.indexOf(x.id) > -1 ? p.picked.filter(y=>y!==x.id) : p.picked.concat(x.id)})})
          })),
          methods: s.cards.map(c=>({
            label:c.brand + ' •••• ' + c.last4, note:'Expires ' + c.exp, icon:'credit_card',
            mark: (method||{}).id===c.id ? 'radio_button_checked' : 'radio_button_unchecked',
            color: (method||{}).id===c.id ? '#0C7F70' : t.dim,
            border: (method||{}).id===c.id ? '#0C7F70' : t.border,
            bg: (method||{}).id===c.id ? 'rgba(12,127,112,.06)' : 'transparent',
            go:()=>this.setState({payModal:Object.assign({}, p, {method:c.id})})
          })).concat([{
            label:'Pay at the library', note:'Cash or card at the desk · a librarian validates it', icon:'storefront',
            mark: isManual ? 'radio_button_checked' : 'radio_button_unchecked',
            color: isManual ? '#0C7F70' : t.dim,
            border: isManual ? '#0C7F70' : t.border,
            bg: isManual ? 'rgba(12,127,112,.06)' : 'transparent',
            go:()=>this.setState({payModal:Object.assign({}, p, {method:'manual'})})
          }]),
          count: picked.length + (picked.length===1 ? ' fine selected' : ' fines selected'),
          total: money(total), hasPick: picked.length > 0,
          methodLabel: isManual ? 'Pay at the library' : (method ? method.brand + ' •••• ' + method.last4 : 'No card on file'),
          payBg: picked.length && chosen ? '#0E5A6E' : t.field,
          payCursor: picked.length && chosen ? 'pointer' : 'not-allowed',
          receipt:p.receipt,
          copyReceipt:this.copier(p.receipt, isManual ? 'Payment code' : 'Receipt'),
          doneBody: isManual
            ? 'Show this code at ' + HOME_LIBRARY + ' and pay ' + (p.charged || money(total)) + ' in cash or by card. The librarian validates it and your account clears within minutes. The code expires in 72 hours.'
            : 'We charged ' + (p.charged || money(total)) + ' to ' + (p.paidWith || (method ? method.brand + ' •••• ' + method.last4 : 'your card')) + '. A receipt is on its way to your inbox.',
          busy: s.busy==='pay', idle: s.busy!=='pay',
          review:()=> picked.length && chosen
            ? this.setState({payModal:Object.assign({}, p, {step:'confirm'})})
            : this.snack(chosen ? 'Select at least one fine to pay.' : 'Choose a payment method first.', 'error'),
          back:()=>this.setState({payModal:Object.assign({}, p, {step:'select'})}),
          addMethod:()=>this.setState({payModal:null, cardModal:{data:Object.assign({}, EMPTY_CARD), confirming:false, dirty:false}}),
          confirm:()=> isManual
            ? this.run('pay', 1200, ()=>{
                const code = 'MP-' + (48210 + s.manuals.length * 7);
                const rec = {code:code, user:userName, email:s.email.trim() || DEMO_EMAIL, kind:'Fine',
                  concept: picked.length===1 ? 'Late fine — ' + picked[0].title : 'Late fines · ' + picked.length + ' titles',
                  amount:total, library:HOME_LIBRARY, created:'Aug 15, 2026 · 11:04', method:'Cash or card at desk',
                  status:'pending', note:'', fineIds:picked.map(x=>x.id), self:true};
                this.setState({
                  manuals:[rec].concat(s.manuals),
                  pendingFines:s.pendingFines.concat(picked.map(x=>x.id)),
                  payModal:Object.assign({}, p, {step:'done', receipt:code, charged:money(total), paidWith:'the library desk'})
                });
                this.snack('Payment code ' + code + ' created for ' + money(total) + '. Pay at the desk within 72 hours.', 'ok');
                this.notify('desk', 'Payment code ' + code + ' is waiting',
                  money(total) + ' to pay at ' + HOME_LIBRARY + ' within 72 hours. The fine clears once a librarian validates it.', 'fines');
              })
            : this.run('pay', 1400, ()=>{
            const rc = 'RC-2026081' + (s.payments.length + 1);
            this.setState({
              paidFines: s.paidFines.concat(picked.map(x=>x.id)),
              payments: [{id:'p'+(s.payments.length+1), date:'Aug 15, 2026',
                desc: picked.length===1 ? 'Late fine — ' + picked[0].title : 'Late fines · ' + picked.length + ' titles',
                method: method.brand + ' •••• ' + method.last4, amount: money(total), receipt:rc}].concat(s.payments),
              payModal: Object.assign({}, p, {step:'done', receipt:rc,
                charged: money(total), paidWith: method.brand + ' •••• ' + method.last4})
            });
            this.snack('Payment of ' + money(total) + ' completed. Your account is up to date.', 'ok');
            this.notify('paid', 'Payment received — ' + money(total),
              (picked.length===1 ? 'Late fine for “' + picked[0].title + '”' : picked.length + ' late fines') +
              ' charged to ' + method.brand + ' •••• ' + method.last4 + '. Receipt ' + rc + '.', 'fines');
          }),
          close:()=>this.setState({payModal:null})
        };
      }).call(this),
      cardModal: (function(){
        const c = s.cardModal;
        if(!c) return null;
        const d = c.data;
        const set = k => e => this.setState({cardModal:Object.assign({}, c, {dirty:true, data:Object.assign({}, d, {[k]:e.target.value})})});
        const digits = String(d.number).replace(/[^0-9]/g,'');
        const valid = !!(d.holder.trim() && digits.length >= 15 && /^[0-9]{2}\/[0-9]{2}$/.test(d.exp) && String(d.cvc).length >= 3);
        const brand = digits.charAt(0)==='4' ? 'Visa' : digits.charAt(0)==='5' ? 'Mastercard' : digits.charAt(0)==='3' ? 'Amex' : 'Card';
        return {
          holder:d.holder, number:d.number, exp:d.exp, cvc:d.cvc, zip:d.zip,
          onHolder:set('holder'), onNumber:set('number'), onExp:set('exp'), onCvc:set('cvc'), onZip:set('zip'),
          primary:d.primary,
          primaryMark: d.primary ? 'check_box' : 'check_box_outline_blank',
          primaryColor: d.primary ? '#0C7F70' : t.dim,
          togglePrimary:()=>this.setState({cardModal:Object.assign({}, c, {dirty:true, data:Object.assign({}, d, {primary:!d.primary})})}),
          brand, preview: digits.length >= 4 ? brand + ' •••• ' + digits.slice(-4) : brand,
          confirming:c.confirming, editing:!c.confirming,
          confirmBody: 'We will store ' + brand + ' •••• ' + (digits.slice(-4) || '••••') + ' for future fines, purchases and delivery charges.' + (d.primary ? ' It becomes your default method.' : ''),
          saveBg: valid ? '#0E5A6E' : t.field, saveCursor: valid ? 'pointer' : 'not-allowed',
          busy: s.busy==='card', idle: s.busy!=='card',
          submit:()=> valid
            ? this.setState({cardModal:Object.assign({}, c, {confirming:true})})
            : this.snack('Check the card number, expiry (MM/YY) and security code.', 'error'),
          back:()=>this.setState({cardModal:Object.assign({}, c, {confirming:false})}),
          confirm:()=>this.run('card', 1200, ()=>{
            const rec = {id:'c'+(s.cards.length+1), brand:brand, last4:digits.slice(-4), exp:d.exp, holder:d.holder, primary:d.primary};
            this.setState({cards: (d.primary ? s.cards.map(x=>Object.assign({}, x, {primary:false})) : s.cards).concat(rec), cardModal:null});
            this.snack(brand + ' •••• ' + digits.slice(-4) + ' added to your payment methods.', 'ok');
          }),
          requestClose:()=> c.dirty ? this.setState({cardExit:true}) : this.setState({cardModal:null})
        };
      }).call(this),
      cardExit: s.cardExit ? {
        stay:()=>this.setState({cardExit:false}),
        leave:()=>{ this.setState({cardModal:null, cardExit:false}); this.snack('Card not saved.', 'info'); }
      } : null,
      cardRemove: (function(){
        if(!s.cardRemove) return null;
        const c = s.cards.filter(x=>x.id===s.cardRemove)[0];
        if(!c) return null;
        return {
          label: c.brand + ' •••• ' + c.last4,
          busy: s.busy==='cardrm', idle: s.busy!=='cardrm',
          stay:()=>this.setState({cardRemove:null}),
          confirm:()=>this.run('cardrm', 800, ()=>{
            const left = s.cards.filter(x=>x.id!==c.id);
            this.setState({cards: (c.primary && left.length) ? left.map((x,i)=>Object.assign({}, x, {primary:i===0})) : left, cardRemove:null});
            this.snack(c.brand + ' •••• ' + c.last4 + ' was removed.', 'info');
          })
        };
      }).call(this),
      prefGroups: PREF_GROUPS.map(g=>({
        label:g.label, note:g.note, icon:g.icon,
        opts: g.opts.map(o=>{
          const on = s.prefs[g.key] === o.v;
          return {label:o.label, note:o.note,
            mark: on ? 'radio_button_checked' : 'radio_button_unchecked',
            bg: on ? 'rgba(14,90,110,.10)' : 'transparent', border: on ? P : t.border,
            go:()=>{ this.setState({prefs:Object.assign({}, s.prefs, {[g.key]:o.v})});
              this.snack(g.label + ' default set to ' + o.label + '.', 'ok'); }};
        })
      })),
      prefSummary: PREF_GROUPS.map(g=>{
        const o = g.opts.filter(x=>x.v===s.prefs[g.key])[0] || g.opts[0];
        return {label:g.label, value:o.label, icon:o.icon};
      }),
      aiPlan, aiLocked: !aiPlan,
      aiCta: isStaff ? 'Configure →' : (aiPlan ? 'See details →' : 'Compare plans →'),
      myLibs: LIBRARIES.filter(l => l.split(' — ')[0] === myCity),
      aiLive: aiPlan && (isStaff || LIBRARIES.filter(l => l.split(' — ')[0] === myCity && liveLibs.indexOf(l) > -1).length > 0),
      aiLibCount: liveLibs.length + ' of ' + LIBRARIES.length + ' libraries',
      upgrade:()=>this.setState({route:'settings'}),
      aiLibs: (isStaff ? scope : []).map(lib => {
        const c = cfgOf(lib) || {provider:'Claude', key:'', on:false};
        const draft = s.aiDraft[lib] !== undefined ? s.aiDraft[lib] : c.key;
        const live = c.on && c.key;
        const setProvider = p => () => this.setState({aiConfig:Object.assign({}, s.aiConfig, {[lib]:Object.assign({}, c, {provider:p})})});
        return {
          name:lib, provider:c.provider, key:draft,
          status: live ? c.provider + ' connected' : 'Not configured',
          statusBg: live ? 'rgba(16,168,140,.14)' : 'rgba(16,38,46,.08)',
          statusFg: live ? '#0C7F70' : t.dim,
          note: live ? 'Members of this library get AI suggestions.' : 'Members here see the non-AI fallback list.',
          providers:['Claude','OpenAI'].map(p=>({label:p, go:setProvider(p),
            bg: c.provider===p ? 'rgba(14,90,110,.14)' : 'transparent',
            fg: c.provider===p ? '#0E5A6E' : t.text,
            border: c.provider===p ? '#0E5A6E' : t.field})),
          onKey:e=>this.setState({aiDraft:Object.assign({}, s.aiDraft, {[lib]:e.target.value})}),
          busy: s.busy==='key:'+lib, idle: s.busy!=='key:'+lib,
          canDisable: !!live,
          save:()=>{
            if(!String(draft).trim()) return this.snack('Enter a valid ' + c.provider + ' key for ' + lib + '.', 'error');
            this.run('key:'+lib, 1200, ()=>{
              this.setState({aiConfig:Object.assign({}, s.aiConfig, {[lib]:{provider:c.provider, key:draft, on:true}})});
              this.snack(c.provider + ' key verified for ' + lib + '. Recommendations enabled for its members.', 'ok');
            });
          },
          disable:()=>{
            this.setState({aiConfig:Object.assign({}, s.aiConfig, {[lib]:Object.assign({}, c, {on:false})})});
            this.snack('AI recommendations turned off for ' + lib + '.', 'info');
          }
        };
      }),
      aiStatus: isStaff
        ? (liveLibs.length ? liveLibs.length + ' of ' + scope.length + ' of your libraries connected' : 'No library connected yet')
        : (!aiPlan ? 'Included from the Plus plan' : (liveLibs.length ? 'Your library is connected' : 'Your library has not enabled it')),
      aiLong: isStaff
        ? 'Keys are set per library in Settings. Each library decides whether its members get model-generated suggestions.'
        : (!aiPlan
          ? 'Personalised picks are part of the Plus and Max plans. Basic keeps full browsing and reservations at your home library, without model-generated suggestions.'
          : liveLibs.length
          ? 'Your library runs this on its own key. The model reads your reservation history and preferred topics, and only suggests titles with copies in the catalog.'
          : 'Your library has not connected a model yet, so these are the most borrowed titles in your genres.'),
      profileRows:[
        {k:'Plan', v: isMember ? (s.role + ' · ' + s.reg.city) : (s.role + ' account'), color:'#0C7F70'},
        {k:'Plan valid until', v: !isMember ? '—' : (s.role==='Basic' ? 'No renewal — Basic is free' : CYCLE.renews), color:t.text},
        {k:'Next renewal', v: !isMember ? '—' : (s.planPending && s.planPending.plan !== s.role
            ? s.planPending.plan + ' from ' + CYCLE.renews
            : (s.role==='Basic' ? 'Nothing scheduled' : 'Renews as ' + s.role + ' on ' + CYCLE.renews)),
          color: s.planPending && s.planPending.plan !== s.role ? '#8A6A28' : t.dim},
        {k:'Reward points', v: s.role==='Max' ? '3,240 pts · redeemable' : 'Max plan only', color: s.role==='Max' ? '#0C7F70' : t.dim},
        {k:'Account status', v:'Fine outstanding', color:'#B3261E'},
        {k:'Fines owed', v: money(dueCents), color: dueCents ? '#B3261E' : '#0F7A63'},
        {k:'Books reserved', v:'4 of 6 allowed', color:t.text},
        {k:'Purchases 2026', v:'11 titles · $248.00', color:t.text},
        {k:'On-time returns', v:'92%', color:'#0F7A63'}
      ],
      topics:['Magical realism','Literary essay','Science fiction','Book history','Biography','Distributed systems'],
      history:[1,2,7,10,5].map(id=>{const b=BOOKS.filter(x=>x.id===id)[0];return {title:b.title, date:'Returned on time', tint:TINTS[(b.id-1)%TINTS.length]};}),
      themeOpts:[
        {label:'Light', note:'Cool paper', icon:'light_mode', go:()=>this.setState({dark:false}),
         bg: s.dark ? 'transparent':'rgba(14,90,110,.08)', border: s.dark ? t.border : P},
        {label:'Dark', note:'Night reading', icon:'dark_mode', go:()=>this.setState({dark:true}),
         bg: s.dark ? 'rgba(14,90,110,.12)':'transparent', border: s.dark ? P : t.border}
      ],
      providers:['Claude','OpenAI','No AI'].map(p=>({label:p, go:()=>this.setState({provider:p}),
        bg: s.provider===p ? 'rgba(14,90,110,.14)':'transparent', fg: s.provider===p ? P : t.text,
        border: s.provider===p ? P : t.field})),
      apiKey:s.apiKey, onKey:e=>this.setState({apiKey:e.target.value, keySaved:false}),
      saveKey:()=>{
        if(!s.apiKey.trim()) return this.snack('Enter a valid '+s.provider+' key.', 'error');
        this.run('key', 1400, ()=>{
          this.setState({keySaved:true});
          this.snack(s.provider+' key verified. Recommendations enabled.', 'ok');
        });
      },
      toggles:[
        {key:'due', label:'Due date reminder', note:'48 h ahead, by email and push'},
        {key:'promos', label:'News and promotions', note:'A weekly digest from the store'},
        {key:'holds', label:'Hold available', note:'When a reserved copy is returned'},
        {key:'digest', label:'Monthly reading recap', note:'Generated with AI from your history'}
      ].map(x=>{
        const on = s.toggles[x.key];
        return Object.assign({}, x, {track: on ? P : (s.dark?'rgba(255,255,255,.24)':'rgba(16,38,46,.24)'),
          justify: on ? 'flex-end':'flex-start',
          go:()=>this.setState({toggles:Object.assign({}, s.toggles, {[x.key]:!on})})});
      }),
      addBook:()=>this.setState({wiz:{step:0, id:null, data:Object.assign({}, EMPTY_BOOK, {branch:scope[0]}), dirty:false}}),
      canAddBooks: isStaff,
      wiz: (function(){
        const w = s.wiz;
        if(!w) return null;
        const d = w.data;
        const set = k => e => this.setState({wiz:Object.assign({}, w, {dirty:true, data:Object.assign({}, d, {[k]:e.target.value})})});
        const ready = w.step===0
          ? !!(d.title.trim() && d.author.trim() && d.isbn.trim())
          : w.step===1 ? !!(String(d.price).trim() && String(d.copies).trim()) : true;
        return {
          step:w.step, stepLabel: WIZ_STEPS[w.step], stepNo: 'Step ' + (w.step+1) + ' of ' + WIZ_STEPS.length,
          editing: !!w.id, heading: w.id ? 'Continue draft' : 'Add a book to the catalog',
          steps: WIZ_STEPS.map((label,i)=>({
            label, n:String(i+1),
            bg: i<=w.step ? '#0E5A6E' : 'transparent',
            fg: i<=w.step ? '#fff' : t.dim,
            border: i<=w.step ? '#0E5A6E' : t.field,
            labelFg: i===w.step ? t.text : t.dim,
            weight: i===w.step ? 600 : 500,
            lineBg: i<w.step ? '#0E5A6E' : t.border,
            hasLine: i < WIZ_STEPS.length-1
          })),
          isDetails:w.step===0, isCopies:w.step===1, isReview:w.step===2,
          title:d.title, author:d.author, isbn:d.isbn, genre:d.genre,
          price:d.price, copies:d.copies, branch:d.branch, tier:d.tier, notes:d.notes,
          onTitle:set('title'), onAuthor:set('author'), onIsbn:set('isbn'), onGenre:set('genre'),
          onPrice:set('price'), onCopies:set('copies'), onBranch:set('branch'), onTier:set('tier'),
          onNotes:set('notes'),
          covr: this.coverBox(d, p => this.setState({wiz:Object.assign({}, w, {dirty:true, data:Object.assign({}, d, p)})}), t),
          genres:['Fiction','Essay','History','Biography','Science fiction','Technical'],
          tiers:['Basic','Plus','Max'],
          branches:scope,
          showBack:w.step>0, showNext:w.step<2, showPublish:w.step===2,
          nextBg: ready ? '#0E5A6E' : t.field, nextCursor: ready ? 'pointer' : 'not-allowed',
          next:()=> ready
            ? this.setState({wiz:Object.assign({}, w, {step:Math.min(2, w.step+1)})})
            : this.snack('Fill in the required fields before continuing.', 'error'),
          back:()=>this.setState({wiz:Object.assign({}, w, {step:Math.max(0, w.step-1)})}),
          review:[
            {k:'Title', v:d.title || '—'}, {k:'Author', v:d.author || '—'},
            {k:'ISBN', v:d.isbn || '—'}, {k:'Genre', v:d.genre},
            {k:'Price', v: d.price ? '$' + d.price : '—'}, {k:'Copies', v:d.copies || '0'},
            {k:'Library', v:d.branch}, {k:'Plan tier', v:d.tier}
          ],
          busy: s.busy==='publish', idle: s.busy!=='publish',
          saveDraft:()=>{ const r = this.upsertBook('draft'); this.snack('Saved “'+r.title+'” as a draft. Pick it up any time from Book management.', 'info'); },
          publish:()=>this.run('publish', 1000, ()=>{ const r = this.upsertBook('catalog'); this.snack('“'+r.title+'” is live in the catalog at ' + r.branch + '.', 'ok'); }),
          requestClose:()=> w.dirty ? this.setState({wizExit:true}) : this.setState({wiz:null})
        };
      }).call(this),
      bookAction: (function(){
        const a = s.bookAction;
        if(!a) return null;
        const b = ALL.filter(x=>x.id===a.id)[0];
        if(!b) return null;
        const d = a.data;
        const set = k => e => this.setState({bookAction:Object.assign({}, a, {dirty:true, data:Object.assign({}, d, {[k]:e.target.value})})});
        const isEdit = a.kind==='edit', isRepair = a.kind==='repair', isDelete = a.kind==='delete';
        const valid = isEdit ? !!(d.title.trim() && d.author.trim() && d.isbn.trim())
          : isRepair ? !!d.reason
          : !!d.reason;
        const apply = () => this.run('action', 900, ()=>{
          if(isEdit){
            const price = String(d.price||'').replace(/[^0-9.]/g,'');
            this.setState({bookEdits:Object.assign({}, s.bookEdits, {[b.id]:{
              title:d.title, author:d.author, isbn:d.isbn, genre:d.genre, tier:d.tier,
              cover:d.cover || null, tint:d.tint || '',
              price: price ? '$'+Number(price).toFixed(2) : b.price,
              copies: (d.copies||'0') + ' / ' + (d.copies||'0'), branch:d.branch
            }}), bookAction:null});
            this.snack('Saved changes to “'+d.title+'”.', 'ok');
          } else if(isRepair){
            this.setState({
              bookStatus:Object.assign({}, s.bookStatus, {[b.id]:'repair'}),
              repairInfo:Object.assign({}, s.repairInfo, {[b.id]:{reason:d.reason, notes:d.notes, back:d.back}}),
              bookAction:null});
            this.snack('“'+b.title+'” sent to repair — ' + d.reason.toLowerCase() + '. Hidden from members until it returns.', 'info');
          } else {
            this.setState({
              bookStatus:Object.assign({}, s.bookStatus, {[b.id]:'deleted'}),
              deleteInfo:Object.assign({}, s.deleteInfo, {[b.id]:{reason:d.reason, notes:d.notes}}),
              bookAction:null});
            this.snack('“'+b.title+'” removed from the catalog — ' + d.reason.toLowerCase() + '.', 'info');
          }
        });
        return {
          isEdit, isRepair, isDelete,
          bookTitle:b.title, bookAuthor:b.author, tint:b.tint, cover:b.cover, hasCover:!!b.cover, coverBg: b.cover ? 'url("' + b.cover + '")' : 'none',
          heading: isEdit ? 'Edit book' : isRepair ? 'Send to repair' : 'Remove from catalog',
          kicker: isEdit ? 'Catalog metadata' : isRepair ? 'Condition report' : 'Withdrawal',
          accent: isDelete ? '#B3261E' : isRepair ? '#1F5F8B' : '#0E5A6E',
          icon: isEdit ? 'edit' : isRepair ? 'build' : 'delete',
          iconBg: isDelete ? 'rgba(179,38,30,.12)' : isRepair ? 'rgba(31,95,139,.16)' : 'rgba(14,90,110,.10)',
          intro: isEdit ? 'Changes apply to every copy of this title across the libraries you manage.'
            : isRepair ? 'Tell the desk what happened. The copy stays out of member search until it comes back.'
            : 'Removing needs a reason for the audit log. Members lose access to this title immediately.',
          title:d.title, author:d.author, isbn:d.isbn, genre:d.genre, price:d.price,
          copies:d.copies, branch:d.branch, tier:d.tier, reason:d.reason, notes:d.notes, back:d.back,
          onTitle:set('title'), onAuthor:set('author'), onIsbn:set('isbn'), onGenre:set('genre'),
          onPrice:set('price'), onCopies:set('copies'), onBranch:set('branch'), onTier:set('tier'),
          onReason:set('reason'), onNotes:set('notes'), onBack:set('back'),
          covr: this.coverBox(d, p => this.setState({bookAction:Object.assign({}, a, {dirty:true, data:Object.assign({}, d, p)})}), t),
          genres:['Fiction','Essay','History','Biography','Science fiction','Technical'],
          tiers:['Basic','Plus','Max'], branches:scope,
          reasons: isRepair ? REPAIR_REASONS : DELETE_REASONS,
          submitLabel: isEdit ? 'Save changes' : isRepair ? 'Send to repair' : 'Remove book',
          submitBg: valid ? (isDelete ? '#B3261E' : '#0E5A6E') : t.field,
          submitCursor: valid ? 'pointer' : 'not-allowed',
          confirming:a.confirming, editing:!a.confirming,
          confirmTitle: isEdit ? 'Save these changes?' : isRepair ? 'Send this copy to repair?' : 'Remove this book?',
          confirmBody: isEdit
            ? 'The catalog entry for “'+b.title+'” will be updated for every member.'
            : isRepair
              ? '“'+b.title+'” moves to In repair with reason “'+d.reason+'”. Active reservations are unaffected.'
              : '“'+b.title+'” moves to Deleted with reason “'+d.reason+'”. This hides it from the catalog and cannot be undone from the member side.',
          confirmCta: isEdit ? 'Yes, save' : isRepair ? 'Yes, send to repair' : 'Yes, remove it',
          busy: s.busy==='action', idle: s.busy!=='action',
          submit:()=> valid
            ? this.setState({bookAction:Object.assign({}, a, {confirming:true})})
            : this.snack(isEdit ? 'Title, author and ISBN are required.' : 'Pick a reason first.', 'error'),
          cancelConfirm:()=>this.setState({bookAction:Object.assign({}, a, {confirming:false})}),
          confirm:apply,
          requestClose:()=> a.dirty ? this.setState({bookExit:true}) : this.setState({bookAction:null})
        };
      }).call(this),
      bookExit: s.bookExit ? {
        stay:()=>this.setState({bookExit:false}),
        leave:()=>{ this.setState({bookAction:null, bookExit:false}); this.snack('Form closed. Nothing was saved.', 'info'); }
      } : null,
      wizExit: s.wizExit ? {
        stay:()=>this.setState({wizExit:false}),
        discard:()=>{ this.setState({wiz:null, wizExit:false}); this.snack('Draft discarded. Nothing was saved.', 'info'); },
        keep:()=>{ const r = this.upsertBook('draft'); this.snack('Saved “'+r.title+'” as a draft.', 'ok'); }
      } : null,
      reserve: (function(){
        if(!s.reserveId) return null;
        const b = shown.filter(x=>x.id===s.reserveId)[0];
        if(!b) return null;
        const rows = b.rows || (bookAccess(b).rows);
        const copies = rows.map((r,i)=>({
          branch: r.branch + ' — ' + r.city,
          count: r.n > 0 ? r.n + (r.n===1?' copy on shelf':' copies on shelf') : 'No copies on shelf',
          ok: r.ok, reason: r.reason,
          mark: i===s.reserveCopy && r.ok ? 'radio_button_checked' : r.ok ? 'radio_button_unchecked' : 'block',
          markColor: !r.ok ? t.dim : i===s.reserveCopy ? '#0C7F70' : t.dim,
          border: i===s.reserveCopy && r.ok ? '#0C7F70' : t.border,
          bg: i===s.reserveCopy && r.ok ? 'rgba(12,127,112,.08)' : 'transparent',
          opacity: r.ok ? 1 : .55,
          cursor: r.ok ? 'pointer' : 'not-allowed',
          go: ()=> r.ok ? this.setState({reserveCopy:i}) : this.snack(r.reason + ' on the ' + plan + ' plan.', 'error')
        }));
        const sel = rows[s.reserveCopy] && rows[s.reserveCopy].ok ? rows[s.reserveCopy] : null;
        const home = s.reserveDelivery==='home';
        return {
          title:b.title, author:b.author, tint:b.tint, cover:b.cover, hasCover:!!b.cover, coverBg: b.cover ? 'url("' + b.cover + '")' : 'none', tier:b.tier, genre:b.genre,
          planNote: plan==='Basic'
            ? 'Basic plan: Basic-catalog titles, ' + (myHome||'home library') + ' only.'
            : plan==='Plus' ? 'Plus plan: any title at ' + myCity + ' libraries.'
            : 'Max plan: any title at any library on the platform.',
          copies, hasSelection: !!sel,
          pickupBg: home ? 'transparent' : 'rgba(12,127,112,.08)', pickupBorder: home ? t.border : '#0C7F70',
          homeBg: home ? 'rgba(12,127,112,.08)' : 'transparent', homeBorder: home ? '#0C7F70' : t.border,
          pickupMark: home ? 'radio_button_unchecked' : 'radio_button_checked',
          homeMark: home ? 'radio_button_checked' : 'radio_button_unchecked',
          pickupNote: sel ? 'Ready in 2 h at ' + sel.branch + ' — ' + sel.city : 'Pick a copy first',
          defaultNote: 'Your default: ' + (s.prefs.delivery==='home' ? 'home delivery' : 'pick up at library'),
          setPickup: ()=>this.setState({reserveDelivery:'pickup'}),
          setHome: ()=>this.setState({reserveDelivery:'home'}),
          feeLabel: home ? '$' + DELIVERY_FEE.toFixed(2) : 'Free',
          total: home ? '$' + DELIVERY_FEE.toFixed(2) : '$0.00',
          dueNote: 'Due in 14 days · Aug 29, 2026',
          confirmBg: sel ? '#0E5A6E' : t.field,
          confirmCursor: sel ? 'pointer' : 'not-allowed',
          close: ()=>this.setState({reserveId:null}),
          busy: s.busy==='reserve', idle: s.busy!=='reserve',
          confirm: ()=>{
            if(!sel) return this.snack('Choose an available copy first.', 'error');
            return this.run('reserve', 1200, ()=>{
            const entry = {
              id:'r'+Date.now(), title:b.title, author:b.author,
              from:'Aug 15, 2026', due:'Aug 29, 2026', dueTs:20260829, fromTs:20260815,
              delivery: home ? 'Home delivery' : 'Pickup — ' + sel.branch, days:0
            };
            this.setState({added:[entry].concat(s.added), reserveId:null, route:'loans'});
            this.load('loans', 700);
            this.notify('hold', 'Reserved “'+b.title+'” for 14 days',
              home ? 'Home delivery in 24–48 h. We notify you again when it ships.' : 'Ready to pick up at ' + sel.branch + '. Due back in 14 days.', 'loans');
            this.snack('Reserved “'+b.title+'” for 14 days · ' + (home ? 'home delivery, $'+DELIVERY_FEE.toFixed(2)+' added to your account.' : 'pick up at ' + sel.branch + ' — ' + sel.city + '.'), 'ok');
            });
          }
        };
      }).call(this),
      rate: rateVals,
      manualPending: money(pendingCents), hasPendingFines: pendingCents > 0,
      pendingCode: (s.manuals.filter(m=>m.self && m.status==='pending')[0] || {}).code || '',
      copyPendingCode: this.copier((s.manuals.filter(m=>m.self && m.status==='pending')[0] || {}).code || '', 'Payment code'),
      manualsLoading:s.loading.manuals, manualsReady:!s.loading.manuals,
      profileLoading:s.loading.profile, profileReady:!s.loading.profile,
      refreshManuals:()=>this.load('manuals', 800),
      manualScope: isSuper ? 'All libraries' : scope.join(' · '),
      manualFilters:['All','Awaiting validation','Validated','Rejected'].map(x=>({
        label:x, go:()=>this.setState({manualFilter:x}),
        bg: s.manualFilter===x ? 'rgba(14,90,110,.14)' : 'transparent',
        fg: s.manualFilter===x ? '#0E5A6E' : t.text,
        border: s.manualFilter===x ? '#0E5A6E' : t.field
      })),
      manualQuery:s.manualQuery,
      onManualQuery:e=>this.setState({manualQuery:e.target.value}),
      manualStats: (function(){
        const inScope = s.manuals.filter(m => isSuper || scope.indexOf(m.library) > -1);
        const pend = inScope.filter(m=>m.status==='pending');
        return [
          {k:'Awaiting validation', v:String(pend.length), color:'#8A6A28'},
          {k:'Amount on hold', v:money(pend.reduce((n,m)=>n+m.amount,0)), color:t.text},
          {k:'Validated today', v:String(inScope.filter(m=>m.status==='validated').length), color:'#0F7A63'},
          {k:'Rejected', v:String(inScope.filter(m=>m.status==='rejected').length), color:'#B3261E'}
        ];
      })(),
      manualRows: (function(){
        const q = String(s.manualQuery).trim().toLowerCase();
        const STATUS = {pending:['Awaiting validation','rgba(224,166,60,.20)','#8A6A28','schedule'],
          validated:['Validated','rgba(15,122,99,.14)','#0F7A63','verified'],
          rejected:['Rejected','rgba(179,38,30,.12)','#B3261E','block']};
        return s.manuals
          .filter(m => isSuper || scope.indexOf(m.library) > -1)
          .filter(m => s.manualFilter==='All' || STATUS[m.status][0] === s.manualFilter)
          .filter(m => !q || (m.code + ' ' + m.user + ' ' + m.email + ' ' + m.concept).toLowerCase().indexOf(q) > -1)
          .map(m => {
            const st = STATUS[m.status];
            return {
              code:m.code, user:m.user, email:m.email, concept:m.concept, library:m.library,
              created:m.created, method:m.method, amount:money(m.amount), note:m.note,
              hasNote: !!m.note,
              kindIcon: m.kind==='Subscription' ? 'card_membership' : 'gavel',
              statusLabel:st[0], statusBg:st[1], statusFg:st[2], statusIcon:st[3],
              isPending: m.status==='pending', settled: m.status!=='pending',
              busy: s.busy==='manual:'+m.code, idle: s.busy!=='manual:'+m.code,
              copyCode: this.copier(m.code, 'Payment code'),
              validate:()=>this.setState({manualAction:{code:m.code, kind:'validate'}}),
              reject:()=>this.setState({manualAction:{code:m.code, kind:'reject'}})
            };
          });
      }).call(this),
      manualEmpty: (function(){
        const q = String(s.manualQuery).trim().toLowerCase();
        const STATUS = {pending:'Awaiting validation', validated:'Validated', rejected:'Rejected'};
        const n = s.manuals
          .filter(m => isSuper || scope.indexOf(m.library) > -1)
          .filter(m => s.manualFilter==='All' || STATUS[m.status] === s.manualFilter)
          .filter(m => !q || (m.code + ' ' + m.user + ' ' + m.email + ' ' + m.concept).toLowerCase().indexOf(q) > -1).length;
        return n === 0 && !s.loading.manuals;
      })(),
      manualAction: (function(){
        const a = s.manualAction;
        if(!a) return null;
        const m = s.manuals.filter(x=>x.code===a.code)[0];
        if(!m) return null;
        const ok = a.kind==='validate';
        return {
          icon: ok ? 'verified' : 'block',
          tint: ok ? '#0F7A63' : '#B3261E',
          title: ok ? 'Validate this payment?' : 'Reject this payment?',
          body: ok
            ? 'Confirm you received ' + money(m.amount) + ' from ' + m.user + ' at the desk. The charge is marked paid and the member is notified.'
            : 'The code stops working and ' + m.user + ' keeps the outstanding balance. Use this when the member never paid at the desk.',
          summary:[{k:'Code', v:m.code}, {k:'Member', v:m.user}, {k:'Concept', v:m.concept}, {k:'Amount', v:money(m.amount)}],
          cta: ok ? 'Validate payment' : 'Reject payment',
          ctaBg: ok ? '#0E5A6E' : '#B3261E',
          busy: s.busy==='manual', idle: s.busy!=='manual',
          close:()=>this.setState({manualAction:null}),
          confirm:()=>this.run('manual', 1100, ()=>{
            const next = s.manuals.map(x => x.code===m.code
              ? Object.assign({}, x, {status: ok ? 'validated' : 'rejected',
                  note: ok ? 'Validated at the desk by ' + userName : 'Rejected at the desk by ' + userName})
              : x);
            const ids = m.fineIds || [];
            const patch = {manuals:next, manualAction:null};
            if(m.self && ids.length){
              patch.pendingFines = s.pendingFines.filter(x=>ids.indexOf(x) < 0);
              if(ok){
                patch.paidFines = s.paidFines.concat(ids);
                patch.payments = [{id:'p'+(s.payments.length+1), date:'Aug 15, 2026', desc:m.concept,
                  method:'Manual · ' + m.code, amount:money(m.amount), receipt:m.code}].concat(s.payments);
              }
            }
            this.setState(patch);
            this.snack(ok
              ? m.code + ' validated. ' + money(m.amount) + ' recorded as paid at the desk.'
              : m.code + ' rejected. The balance stays outstanding.', ok ? 'ok' : 'info');
            if(m.self) this.notify(ok ? 'paid' : 'pending',
              ok ? 'Payment validated — ' + money(m.amount) : 'Payment ' + m.code + ' was rejected',
              ok ? m.concept + ' is settled. ' + m.code + ' was validated at ' + m.library + '.'
                 : 'The desk did not receive the money, so the balance stays outstanding. Generate a new code or pay by card.', 'fines');
          })
        };
      }).call(this),
      openManualNew:()=>this.setState({manualNew:Object.assign({}, EMPTY_MANUAL, {library: isSuper ? LIBRARIES[0] : scope[0]})}),
      manualNew: (function(){
        const n = s.manualNew;
        if(!n) return null;
        const set = k => e => this.setState({manualNew:Object.assign({}, n, {[k]:e.target.value})});
        const member = USERS.filter(u=>u.email===n.email)[0] || null;
        const amt = String(n.amount).replace(/[^0-9.]/g,'');
        const valid = !!(member && amt && Number(amt) > 0);
        return {
          email:n.email, onEmail:set('email'), amount:n.amount, onAmount:set('amount'),
          note:n.note, onNote:set('note'),
          people: USERS.filter(u=>['Basic','Plus','Max'].indexOf(u.role) > -1)
            .filter(u => isSuper || scope.indexOf(u.library) > -1)
            .map(u=>({value:u.email, label:u.name + ' · ' + u.role})),
          memberLine: member ? member.name + ' · ' + member.role + ' · ' + member.library : 'Pick the member standing at the desk.',
          hasMember: !!member,
          kinds:[{k:'Fine', label:'Late fine', icon:'gavel'}, {k:'Subscription', label:'Subscription', icon:'card_membership'}].map(x=>({
            label:x.label, icon:x.icon,
            go:()=>this.setState({manualNew:Object.assign({}, n, {kind:x.k})}),
            bg: n.kind===x.k ? 'rgba(14,90,110,.14)' : 'transparent',
            fg: n.kind===x.k ? '#0E5A6E' : t.text,
            border: n.kind===x.k ? '#0E5A6E' : t.field
          })),
          isSub: n.kind==='Subscription',
          plans:['Basic','Plus','Max'].map(p=>({
            label:p, go:()=>this.setState({manualNew:Object.assign({}, n, {plan:p})}),
            bg: n.plan===p ? 'rgba(14,90,110,.14)' : 'transparent',
            fg: n.plan===p ? '#0E5A6E' : t.text,
            border: n.plan===p ? '#0E5A6E' : t.field
          })),
          methods:['Cash at desk','Card at desk'].map(mm=>({
            label:mm, go:()=>this.setState({manualNew:Object.assign({}, n, {method:mm})}),
            bg: n.method===mm ? 'rgba(14,90,110,.14)' : 'transparent',
            fg: n.method===mm ? '#0E5A6E' : t.text,
            border: n.method===mm ? '#0E5A6E' : t.field
          })),
          saveBg: valid ? '#0E5A6E' : t.field, saveCursor: valid ? 'pointer' : 'not-allowed',
          busy: s.busy==='manual-new', idle: s.busy!=='manual-new',
          close:()=>this.setState({manualNew:null}),
          save:()=> valid
            ? this.run('manual-new', 1100, ()=>{
                const code = 'MP-' + (48210 + s.manuals.length * 7);
                const rec = {code:code, user:member.name, email:member.email, kind:n.kind,
                  concept: n.kind==='Subscription' ? 'Subscription — ' + n.plan + ' · 1 month' : 'Late fine settled at desk',
                  amount: Math.round(Number(amt) * 100), library:member.library,
                  created:'Aug 15, 2026 · 11:20', method:n.method, status:'validated',
                  note:'Taken and validated by ' + userName + (n.note ? ' · ' + n.note : '')};
                this.setState({manuals:[rec].concat(s.manuals), manualNew:null});
                this.snack(money(rec.amount) + ' recorded for ' + member.name + ' as ' + code + '.', 'ok');
              })
            : this.snack(member ? 'Enter the amount you received.' : 'Pick the member first.', 'error')
        };
      }).call(this),
      detail: (function(){
        const b = s.detailId ? (shown.filter(x=>x.id===s.detailId)[0] || null) : null;
        if(!b) return null;
        const SECTION = {'Fiction':'Literature', 'Essay':'Essays & criticism', 'History':'History',
          'Science fiction':'Speculative fiction', 'Biography':'Biography & memoir', 'Technical':'Computing'};
        const CODE = {'Fiction':'FIC', 'Essay':'ESS', 'History':'HIS', 'Science fiction':'SCF', 'Biography':'BIO', 'Technical':'TEC'};
        const PUB = ['Alfaguara', 'Penguin Classics', 'Faber & Faber', 'Anagrama', 'Vintage', 'O\u2019Reilly'];
        const parts = String(b.copies || '0 / 0').split(' / ');
        const free = parseInt(parts[0], 10) || 0, total = parseInt(parts[1], 10) || 0;
        const aisle = 3 + (b.id * 7) % 22;
        const floor = 1 + (b.id % 4);
        const shelfLetter = 'ABCDEF'.charAt(b.id % 6);
        const holds = b.id % 4;
        const borrowed = 38 + b.id * 13;
        const author3 = String(b.author).split(' ').slice(-1)[0].slice(0,3).toUpperCase();
        return Object.assign({}, b, {
          onBuy: ()=>this.setState({buyModal:{id:b.id, fulfil:s.prefs.purchase, method:(defaultCard||{}).id}, detailId:null}),
          section: SECTION[b.genre] || 'General collection',
          callNumber: (CODE[b.genre] || 'GEN') + ' ' + author3 + ' ' + b.year,
          shelfLine: 'Floor ' + floor + ' · Aisle ' + aisle + ' · Shelf ' + shelfLetter,
          shelfNote: free > 0
            ? free + (free === 1 ? ' copy is on the shelf right now.' : ' copies are on the shelf right now.')
            : 'Every copy is on loan. Reserve to join the queue.',
          record:[
            {k:'ISBN', v:b.isbn, copy:this.copier(b.isbn, 'ISBN'), hasCopy:true},
            {k:'Publisher', v:PUB[b.id % PUB.length]},
            {k:'Edition', v:(1 + b.id % 5) + (b.id % 5 === 0 ? 'st' : b.id % 5 === 1 ? 'nd' : 'rd') + ' edition · ' + b.year},
            {k:'Language', v: b.id % 3 === 0 ? 'Spanish' : 'English'},
            {k:'Format', v: b.pages > 500 ? 'Hardcover' : 'Paperback'},
            {k:'Pages', v: b.pages ? String(b.pages) + ' pages' : '—'},
            {k:'Added to catalog', v:'Mar ' + (1 + b.id % 27) + ', 202' + (2 + b.id % 4)},
            {k:'Condition', v: b.id % 5 === 0 ? 'Fair · repaired 2025' : 'Good'}
          ],
          avail:[
            {k:'On shelf', v: free + ' of ' + total},
            {k:'On loan', v: String(Math.max(total - free, 0))},
            {k:'Holds queued', v: String(holds)},
            {k:'Times borrowed', v: String(borrowed)}
          ]
        });
      }).call(this),
      closeBook:e=>{ if(!e || !e.target || e.target===e.currentTarget || e.currentTarget.tagName==='BUTTON') this.setState({detailId:null}); },
      socials:[
        {label:'Instagram', icon:'photo_camera', href:'#instagram'},
        {label:'X', icon:'alternate_email', href:'#x'},
        {label:'Facebook', icon:'groups', href:'#facebook'},
        {label:'YouTube', icon:'smart_display', href:'#youtube'},
        {label:'Newsletter', icon:'mail', href:'#newsletter'}
      ]
    };
  }
}
