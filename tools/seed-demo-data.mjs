/**
 * Adds leads and units so the pipeline board, the unit grid and the dashboard charts have enough
 * to work with. Projects and an admin account must already exist -- run the API once with `--init`
 * (or in Development, just run it) so roles and the initial admin are seeded.
 *
 *   node tools/seed-demo-data.mjs
 *
 * Environment overrides:
 *
 *   CRM_API_URL   default http://localhost:5063/api
 *   CRM_EMAIL     default admin@realestatecrm.local
 *   CRM_PASSWORD  default Admin@12345
 *
 * Goes through the HTTP API rather than SQL on purpose. That is what makes the plan limits real:
 * the Free plan caps units at 25 and the API answers 402 past that. This script treats that as a
 * normal stop rather than an error, because it is the product working. Writing rows straight into
 * the table would sail past the cap and leave the tenant in a state the application considers
 * invalid.
 *
 * Enums travel as numbers: LeadSource Website=0, Facebook=1, Instagram=2, Google=3, Referral=4,
 * WalkIn=5, Phone=6, Other=7. UnitStatus Available=0, Reserved=1, Sold=2, Unavailable=3.
 */

const API = process.env.CRM_API_URL ?? 'http://localhost:5063/api';
const EMAIL = process.env.CRM_EMAIL ?? 'admin@realestatecrm.local';
const PASSWORD = process.env.CRM_PASSWORD ?? 'Admin@12345';

let token = '';
const stats = { created: 0, failed: [], planLimited: 0 };

async function call(method, path, body) {
  let res;
  try {
    res = await fetch(API + path, {
      method,
      headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (cause) {
    throw new Error(`Could not reach the API at ${API}. Is it running?`, { cause });
  }
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch {}
  return { ok: res.ok, status: res.status, json, text };
}

async function create(label, path, body) {
  const r = await call('POST', path, body);
  if (r.ok) { stats.created++; return r.json; }
  // 402 is the subscription plan's entity cap. Expected, not a failure.
  if (r.status === 402) { stats.planLimited++; return null; }
  stats.failed.push(`${label}: ${r.status} ${r.text.slice(0, 150)}`);
  return null;
}

const listOf = res => (Array.isArray(res.json) ? res.json : res.json?.items ?? []);

const NAMES = ['Mahmoud Fahmy', 'Nadia Serry', 'Bassem Ragab', 'Heba Kamel', 'Wael Abdallah',
  'Injy Mostafa', 'Sherif Lotfy', 'Rasha Tawfik', 'Ehab Zaki', 'Mai Gharib',
  'Ashraf Selim', 'Doaa Hegazy', 'Fady Nassif', 'Ghada Sobhy', 'Hesham Badr',
  'Iman Refaat', 'Kareem Wahba', 'Laila Shafik', 'Mostafa Diab', 'Nihal Fawzy',
  'Osama Helmy', 'Passant Amin', 'Ramy Fouad', 'Salwa Nagy'];
const AREAS = ['New Cairo', 'Sheikh Zayed', 'Maadi', '6th of October', 'North Coast',
               'Zamalek', 'Heliopolis', 'New Capital'];
const TYPES = ['Apartment', 'Villa', 'Townhouse', 'Duplex', 'Studio', 'Penthouse'];
const NOTES = [
  'Wants delivery within 18 months.',
  'Cash buyer, negotiating on price.',
  'Comparing against two other developers.',
  'Needs a ground floor with a garden.',
  'Investor, looking at rental yield.',
  'Relocating from abroad next quarter.',
];

async function main() {
  const login = await call('POST', '/auth/login', { email: EMAIL, password: PASSWORD });
  if (!login.ok) {
    throw new Error(`Login failed for ${EMAIL} (${login.status}). ${login.text.slice(0, 180)}`);
  }
  token = login.json.accessToken ?? login.json.token;
  if (!token) throw new Error('Login returned no token: ' + JSON.stringify(login.json).slice(0, 200));

  // Leads and units carry no natural key, so the API cannot reject a duplicate and a second run
  // would just pile more on. Stop if the tenant already looks seeded.
  // Read totalCount, not the length of the page. Asking for pageSize=1 and then checking the
  // array length means the guard can never see more than one, which is how a "skip if already
  // seeded" check silently seeded twice.
  const leadPage = await call('GET', '/leads?pageSize=1&page=1');
  const leadCount = leadPage.json?.totalCount ?? listOf(leadPage).length;
  if (leadCount >= 20) {
    console.log(`This tenant already has ${leadCount} leads; leaving it alone.`);
    return;
  }

  for (const [i, fullName] of NAMES.entries()) {
    const min = 1_500_000 + (i % 6) * 750_000;
    await create(`lead ${fullName}`, '/leads', {
      fullName,
      phone: `010${String(20000000 + i * 131071).slice(0, 8)}`,
      email: `${fullName.toLowerCase().replace(/[^a-z]+/g, '.')}@example.com`,
      source: i % 8,
      budgetMin: min,
      budgetMax: min + 1_250_000,
      preferredLocation: AREAS[i % AREAS.length],
      propertyType: TYPES[i % TYPES.length],
      assignedAgentId: null,
      notes: NOTES[i % NOTES.length],
    });
  }

  // Projects are discovered, not hard-coded: their ids differ per database.
  const projects = listOf(await call('GET', '/projects'));
  if (!projects.length) {
    console.log('No projects exist, so no units were added. Create a project first.');
  }

  for (const [p, project] of projects.entries()) {
    const code = project.name.split(/\s+/).map(w => w[0]).join('').toUpperCase().slice(0, 2) || `P${p}`;
    for (let n = 1; n <= 12; n++) {
      const beds = 1 + (n % 4);
      const area = 90 + beds * 45 + (n % 3) * 15;
      // A mix of statuses, so the status filter and the "available units" figure mean something.
      const status = n % 6 === 0 ? 2 : n % 5 === 0 ? 1 : 0;
      const price = Math.round((area * 42_000 + (n % 4) * 300_000) / 1000) * 1000;
      await create(`unit ${code}-${n}`, '/units', {
        projectId: project.id,
        unitCode: `${code}-${String(100 + n)}`,
        propertyType: TYPES[n % TYPES.length],
        price,
        area,
        bedrooms: beds,
        bathrooms: Math.max(1, beds - 1),
        floor: `${n % 9}`,
        location: AREAS[n % AREAS.length],
        status,
        downPayment: Math.round(price * 0.15),
        installmentYears: [5, 7, 8, 10][n % 4],
        description: `${beds}-bedroom ${TYPES[n % TYPES.length].toLowerCase()} of ${area} m².`,
        isPubliclyListed: status === 0,
      });
    }
  }

  console.log(`created ${stats.created}, failed ${stats.failed.length}`);
  if (stats.planLimited) {
    console.log(`${stats.planLimited} units were not added because the subscription plan's cap was reached. That is the plan limit working, not an error.`);
  }
  stats.failed.slice(0, 10).forEach(f => console.log('  ! ' + f));
  if (stats.failed.length) process.exitCode = 1;
}

main().catch(err => {
  console.error(err.message);
  if (err.cause) console.error('  cause:', err.cause.message);
  process.exit(1);
});
