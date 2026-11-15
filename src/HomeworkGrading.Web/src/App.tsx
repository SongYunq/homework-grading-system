import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, Dialog, Input, Pagination, Table, Tag } from 'tdesign-react';
import { ArrowLeftRight1Icon, ArrowRightIcon, BookOpenIcon, CheckCircleIcon, ChevronRightIcon, FileIcon, PlusIcon, TimeIcon } from 'tdesign-icons-react';
import { GradingView, StudentView } from './SecondaryViews';
import { assignments, formatDate, statusLabels } from './demoAssignments';
import type { AssignmentStatus, DemoAssignment } from './demoAssignments';

type View = 'dashboard' | 'grading' | 'student';
type Filter = 'all' | AssignmentStatus;

const filters: Array<{ key: Filter; label: string }> = [
  { key: 'all', label: '全部' },
  { key: 'pending', label: '待批改' },
  { key: 'open', label: '待提交' },
  { key: 'graded', label: '已批改' },
  { key: 'closed', label: '已截止' },
];

const subjects: Record<string, { tone: string; glyph: string }> = {
  数学: { tone: 'blue', glyph: '数' },
  语文: { tone: 'rose', glyph: '文' },
  英语: { tone: 'violet', glyph: 'A' },
  物理: { tone: 'mint', glyph: '理' },
  历史: { tone: 'amber', glyph: '史' },
  地理: { tone: 'blue', glyph: '地' },
};

function HeroArt() {
  return (
    <svg className="hero-art" viewBox="0 0 350 200" fill="none" aria-hidden="true">
      <defs>
        <linearGradient id="paper" x1="130" y1="36" x2="250" y2="185" gradientUnits="userSpaceOnUse">
          <stop stopColor="#fff" /><stop offset="1" stopColor="#dce8ff" />
        </linearGradient>
        <linearGradient id="tile" x1="245" y1="102" x2="301" y2="161" gradientUnits="userSpaceOnUse">
          <stop stopColor="#aeceff" /><stop offset="1" stopColor="#6998f8" />
        </linearGradient>
        <pattern id="hero-grid" width="19" height="19" patternUnits="userSpaceOnUse">
          <path d="M19 0H0v19" stroke="#a9c4ff" opacity=".26" />
        </pattern>
      </defs>
      <rect x="19" y="10" width="323" height="185" rx="26" fill="url(#hero-grid)" />
      <ellipse cx="204" cy="104" rx="132" ry="68" transform="rotate(-19 204 104)" stroke="#9cbdff" strokeOpacity=".48" />
      <ellipse cx="204" cy="104" rx="105" ry="55" transform="rotate(-19 204 104)" stroke="#9cbdff" strokeOpacity=".31" />
      <path d="M61 70h14m-7-7v14m231-23h12m-6-6v12M108 156h12m-6-6v12" stroke="#a7c4ff" strokeWidth="1.5" strokeLinecap="round" />
      <circle cx="92" cy="106" r="3" fill="#a8c5ff" /><circle cx="282" cy="31" r="4" fill="#a8c5ff" /><circle cx="315" cy="140" r="3" fill="#d7e5ff" />
      <g transform="rotate(14 211 109)">
        <rect x="156" y="47" width="106" height="127" rx="14" fill="#73a4ff" fillOpacity=".65" stroke="#c1d5ff" strokeOpacity=".68" />
      </g>
      <g transform="rotate(-12 193 103)">
        <rect x="136" y="40" width="113" height="136" rx="15" fill="#0c328b" fillOpacity=".2" transform="translate(4 8)" />
        <rect x="136" y="40" width="113" height="136" rx="15" fill="url(#paper)" />
        <path d="M154 63h36" stroke="#315bd0" strokeWidth="5" strokeLinecap="round" />
        <path d="M154 77h51" stroke="#b0c5f4" strokeWidth="3" strokeLinecap="round" />
        <rect x="154" y="91" width="77" height="63" rx="8" fill="#ecf3ff" stroke="#c2d5fb" />
        <path d="M167 103v38h51m-45-7 13-9 12 3 15-17m-9 1h9v9" stroke="#4c78e6" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
      </g>
      <g transform="rotate(9 270 128)">
        <rect x="241" y="100" width="58" height="57" rx="15" fill="#123b9c" fillOpacity=".26" transform="translate(3 6)" />
        <rect x="241" y="100" width="58" height="57" rx="15" fill="url(#tile)" stroke="#c5dcff" />
        <path d="m256 129 9 9 18-19" stroke="#fff" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round" />
      </g>
    </svg>
  );
}

function StatusTag({ status }: { status: AssignmentStatus }) {
  return <Tag size="small" variant="light" shape="round" className={`status-pill status-${status}`}><i />{statusLabels[status]}</Tag>;
}

function SubjectMark({ subject }: { subject: string }) {
  const { tone, glyph } = subjects[subject] ?? { tone: 'blue', glyph: subject.slice(0, 1) };
  return <span className={`subject-mark tone-${tone}`} aria-hidden="true">{glyph}</span>;
}

function Dashboard({ onGrade }: { onGrade: () => void }) {
  const [rows, setRows] = useState(assignments);
  const [filter, setFilter] = useState<Filter>('all');
  const [page, setPage] = useState(1);
  const [detail, setDetail] = useState<DemoAssignment | null>(null);
  const [publishing, setPublishing] = useState(false);
  const [title, setTitle] = useState('');
  const [subject, setSubject] = useState('');
  const [notice, setNotice] = useState('');
  const filtered = filter === 'all' ? rows : rows.filter(row => row.status === filter);
  const pageSize = 5;
  const visible = filtered.slice((page - 1) * pageSize, page * pageSize);
  const count = (key: Filter) => key === 'all' ? rows.length : rows.filter(row => row.status === key).length;
  const selectFilter = (key: Filter) => { setFilter(key); setPage(1); };

  const columns = [
    { colKey: 'title', title: '作业名称', width: 258, cell: ({ row }: { row: DemoAssignment }) => (
      <button className="assignment-title" onClick={() => setDetail(row)}>
        <SubjectMark subject={row.subject} /><span>{row.title}</span>
      </button>
    ) },
    { colKey: 'subject', title: '学科', width: 74, cell: ({ row }: { row: DemoAssignment }) => <span className="cell-muted">{row.subject}</span> },
    { colKey: 'dueAt', title: '截止时间', width: 122, cell: ({ row }: { row: DemoAssignment }) => <span className="cell-muted tabular">{formatDate(row.dueAt)}</span> },
    { colKey: 'progress', title: '批改进度', width: 134, cell: ({ row }: { row: DemoAssignment }) => (
      <div className="table-progress">
        <span className={`mini-bar ${row.status === 'graded' ? 'success' : ''}`} aria-hidden="true"><i style={{ width: `${row.submitted ? row.graded / row.submitted * 100 : 0}%` }} /></span>
        <span className="tabular" aria-label={`已批改 ${row.graded} 份，共提交 ${row.submitted} 份`}>{row.graded}/{row.submitted}</span>
      </div>
    ) },
    { colKey: 'status', title: '状态', width: 95, cell: ({ row }: { row: DemoAssignment }) => <StatusTag status={row.status} /> },
    { colKey: 'action', title: '操作', width: 90, cell: ({ row }: { row: DemoAssignment }) => (
      <Button variant="text" size="small" theme="primary" onClick={() => row.id === 'math' ? onGrade() : setDetail(row)}>
        {row.id === 'math' ? '去批改' : '查看'}
      </Button>
    ) },
  ];

  function publish(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!title.trim() || !subject.trim()) return;
    const data = new FormData(event.currentTarget);
    const dueAt = String(data.get('dueAt') ?? '');
    if (!dueAt) return;
    setRows([{ id: `demo-${Date.now()}`, title: title.trim(), subject: subject.trim(), dueAt, submitted: 0, graded: 0, status: 'open' }, ...rows]);
    selectFilter('all');
    setPublishing(false);
    setNotice('演示作业已添加到列表');
  }

  return (
    <div className="dashboard">
      <section className="hero" aria-labelledby="dashboard-title">
        <div className="hero-copy">
          <span className="hero-kicker"><BookOpenIcon size="15px" aria-hidden="true" /> 教学工作台</span>
          <h1 id="dashboard-title">我的作业</h1>
          <p>在这里发布作业、跟进提交，并完成批改。</p>
        </div>
        <HeroArt />
      </section>

      <section className="summary-grid" aria-label="作业概览">
        {([
          { key: 'all', label: '全部作业', hint: '作业总数', icon: FileIcon, tone: 'blue' },
          { key: 'pending', label: '待批改', hint: '仍有作答待批改', icon: TimeIcon, tone: 'amber' },
          { key: 'graded', label: '已批改', hint: '收到的作答均已批改', icon: CheckCircleIcon, tone: 'mint' },
        ] as const).map(({ key, label, hint, icon: Icon, tone }) => (
          <button className="summary-card" key={key} onClick={() => selectFilter(key)} aria-pressed={filter === key}>
            <span className={`stat-icon tone-${tone}`}><Icon size="21px" aria-hidden="true" /></span>
            <span className="summary-copy"><span>{label}</span><strong>{String(count(key)).padStart(2, '0')}</strong><small>{hint}</small></span>
            <ChevronRightIcon className="summary-arrow" size="17px" aria-hidden="true" />
          </button>
        ))}
      </section>

      <section className="list-panel" aria-labelledby="list-title">
        <div className="list-head">
          <div><h2 id="list-title">作业列表</h2><p>查看提交和批改进度，或发布一份新作业。</p></div>
          <Button theme="primary" icon={<PlusIcon />} onClick={() => { setTitle(''); setSubject(''); setPublishing(true); }}>发布作业</Button>
        </div>
        <div className="notice" role="status">{notice}</div>
        <div className="filter-strip" aria-label="按状态筛选">
          {filters.map(({ key, label }) => (
            <button key={key} className={filter === key ? 'selected' : ''} aria-pressed={filter === key} onClick={() => selectFilter(key)}>
              {label}<span>{count(key)}</span>
            </button>
          ))}
        </div>
        <Table className="assignment-table" rowKey="id" columns={columns} data={visible} hover empty="当前筛选下没有作业" />
        <div className="mobile-assignments">
          {visible.length === 0 && <p className="mobile-empty">当前筛选下没有作业</p>}
          {visible.map(row => (
            <article className="mobile-assignment" key={row.id}>
              <button className="assignment-title" onClick={() => setDetail(row)}><SubjectMark subject={row.subject} /><span>{row.title}</span></button>
              <StatusTag status={row.status} />
              <p>{row.subject} · {formatDate(row.dueAt)} 截止</p>
              <Button variant="text" size="small" theme="primary" onClick={() => row.id === 'math' ? onGrade() : setDetail(row)}>{row.id === 'math' ? '去批改' : '查看'}</Button>
            </article>
          ))}
        </div>
        <div className="list-footer">
          <span>共 {filtered.length} 份作业</span>
          <Pagination size="small" total={filtered.length} current={page} pageSize={pageSize} totalContent={false} showPageSize={false} onCurrentChange={setPage} />
        </div>
      </section>

      {detail && <Dialog visible header={detail.title} onClose={() => setDetail(null)} footer={false}>
        <div className="detail-dialog">
          <StatusTag status={detail.status} />
          <dl><dt>学科</dt><dd>{detail.subject}</dd><dt>截止时间</dt><dd>{formatDate(detail.dueAt)}</dd><dt>已批改 / 已提交</dt><dd>{detail.graded} / {detail.submitted}</dd></dl>
          {detail.id === 'math' && <Button theme="primary" icon={<ArrowRightIcon />} onClick={onGrade}>进入批改</Button>}
        </div>
      </Dialog>}
      <Dialog visible={publishing} header="发布作业" onClose={() => setPublishing(false)} footer={false}>
        <form className="publish-form" onSubmit={publish}>
          <label>作业名称<Input value={title} onChange={setTitle} placeholder="例如：数学周练 · 一次函数" maxlength={160} /></label>
          <label>学科<Input value={subject} onChange={setSubject} placeholder="例如：数学" maxlength={40} /></label>
          <label>截止时间<input className="date-input" type="datetime-local" name="dueAt" defaultValue="2026-11-22T20:00" required /></label>
          <div className="dialog-actions"><Button type="button" variant="outline" onClick={() => setPublishing(false)}>取消</Button><Button type="submit" theme="primary" disabled={!title.trim() || !subject.trim()}>添加演示作业</Button></div>
        </form>
      </Dialog>
    </div>
  );
}

export default function App() {
  const [view, setView] = useState<View>('dashboard');
  const student = view === 'student';
  function go(next: View) {
    setView(next);
    window.scrollTo(0, 0);
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main">跳到主要内容</a>
      <header className="app-header">
        <a className="brand" href="#" onClick={event => { event.preventDefault(); go(student ? 'student' : 'dashboard'); }}>
          <img src="./favicon.svg" alt="" width="36" height="36" />
          <span>课业台<small>作业批改</small></span>
        </a>
        <div className="header-actions">
          <Tag size="small" variant="outline" className="demo-tag">样式预览</Tag>
          <Button variant="outline" theme="default" icon={<ArrowLeftRight1Icon />} onClick={() => go(student ? 'dashboard' : 'student')}>{student ? '切换教师端' : '切换学生端'}</Button>
          <div className="header-profile" aria-label={student ? '当前视角：林小满，学生' : '当前视角：周老师，教师'}>
            <span className={`avatar ${student ? 'student-avatar' : ''}`}>{student ? '林' : '周'}</span><span><b>{student ? '林小满' : '周老师'}</b><small>{student ? '学生' : '教师'}</small></span>
          </div>
        </div>
      </header>
      <aside className="app-sidebar">
        <div className="nav-label">{student ? '学习空间' : '教学管理'}</div>
        <nav aria-label={student ? '学生导航' : '教师导航'}>
          {!student && <>
            <button className={view === 'dashboard' ? 'active' : ''} aria-current={view === 'dashboard' ? 'page' : undefined} onClick={() => go('dashboard')}><FileIcon size="19px" /> 作业管理</button>
            <button className={view === 'grading' ? 'active' : ''} aria-current={view === 'grading' ? 'page' : undefined} onClick={() => go('grading')}><CheckCircleIcon size="19px" /> 批改示例</button>
          </>}
          {student && <button className="active" aria-current="page" onClick={() => go('student')}><BookOpenIcon size="19px" /> 我的作业</button>}
        </nav>
        <div className="sidebar-end"><span /> {student ? '学生工作区' : '教师工作区'}</div>
      </aside>
      <main id="main" className="app-main" tabIndex={-1}>
        {view === 'dashboard' && <Dashboard onGrade={() => go('grading')} />}
        {view === 'grading' && <GradingView onBack={() => go('dashboard')} />}
        {view === 'student' && <StudentView />}
      </main>
    </div>
  );
}
