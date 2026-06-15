import { FileCog, LogOut, UserRoundCog } from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/model/useAuth'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'

const navItems = [
  { to: '/groups', label: 'Групи' },
  { to: '/generator', label: 'Генератор' },
]

const managementNavItems = [
  { to: '/management?tab=degrees', value: 'degrees', label: 'Ступені' },
  { to: '/management?tab=positions', value: 'positions', label: 'Посади' },
  { to: '/management?tab=specialties', value: 'specialties', label: 'Спеціальності' },
  { to: '/management?tab=commission-heads', value: 'commission-heads', label: 'Голови ДЕК' },
]

export function AppShell() {
  const { logout, secretary } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [isLogoutOpen, setIsLogoutOpen] = useState(false)
  const isManagementPage = location.pathname.startsWith('/management')
  const managementTab = new URLSearchParams(location.search).get('tab') ?? 'specialties'

  const confirmLogout = () => {
    logout()
    setIsLogoutOpen(false)
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen bg-transparent text-slate-600">
      <header className="mx-auto flex w-full max-w-[1280px] items-center justify-between px-9 py-9">
        <NavLink
          to="/groups"
          aria-label="На головну"
          className="grid size-14 place-items-center rounded-[13px] bg-blue-600 font-['Plaster'] text-[42px] font-normal leading-none text-white shadow-sm"
        >
          K
        </NavLink>

        {isManagementPage ? (
          <nav className="flex h-20 overflow-hidden rounded-full border border-slate-300/55 bg-slate-50/70 p-2 shadow-[0_3px_13px_rgba(58,71,88,0.24)] backdrop-blur">
            {managementNavItems.map((item) => (
              <NavLink
                key={item.value}
                to={item.to}
                className={[
                  'grid h-16 min-w-52 place-items-center rounded-full px-6 text-3xl font-bold transition',
                  managementTab === item.value
                    ? 'bg-blue-600 text-white shadow-sm'
                    : 'text-slate-500 [text-shadow:0_1px_0_rgba(255,255,255,0.95)] hover:bg-white/60',
                ].join(' ')}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        ) : (
          <nav className="flex h-20 overflow-hidden rounded-full border border-slate-300/55 bg-slate-50/70 p-2 shadow-[0_3px_13px_rgba(58,71,88,0.24)] backdrop-blur">
            {navItems.map((item, index) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  [
                    'grid h-16 min-w-48 place-items-center text-3xl font-bold transition',
                    index > 0 ? 'border-l border-slate-300/45' : '',
                    isActive
                      ? 'rounded-full border-l-0 bg-blue-600 text-white shadow-sm'
                      : 'text-slate-500 [text-shadow:0_1px_0_rgba(255,255,255,0.95)] hover:bg-white/60',
                  ].join(' ')
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        )}

        <div className="flex items-center gap-5">
          {secretary?.isSuperSecretary && (
            <NavLink
              to={isManagementPage ? '/groups' : '/management'}
              aria-label={isManagementPage ? 'Панель секретаря' : 'Панель супер-секретаря'}
              title={isManagementPage ? 'Панель секретаря' : 'Панель супер-секретаря'}
              className={({ isActive }) =>
                [
                  'grid size-14 place-items-center rounded-full text-white shadow-sm transition focus:outline-none focus:ring-4 focus:ring-orange-100',
                  isActive || isManagementPage ? 'bg-orange-600' : 'bg-orange-500 hover:bg-orange-600',
                ].join(' ')
              }
            >
              <FileCog size={31} strokeWidth={2.4} />
            </NavLink>
          )}
          <div
            title={`${secretary?.fullName ?? ''}${secretary?.specialtyName ? ` · ${secretary.specialtyName}` : ''}`}
            className="grid size-14 place-items-center rounded-full bg-blue-600 text-white shadow-sm"
          >
            <UserRoundCog size={31} strokeWidth={2.4} />
          </div>
          <button
            type="button"
            aria-label="Вийти"
            onClick={() => setIsLogoutOpen(true)}
            className="grid size-14 place-items-center rounded-full bg-red-100 text-red-500 transition hover:bg-red-200 focus:outline-none focus:ring-4 focus:ring-red-200"
          >
            <LogOut size={30} strokeWidth={2.6} />
          </button>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[1280px] px-9 pb-14">
        <Outlet />
      </main>

      {isLogoutOpen && (
        <ConfirmDialog
          title="Вихід із системи"
          confirmLabel="Вийти"
          onConfirm={confirmLogout}
          onCancel={() => setIsLogoutOpen(false)}
        >
          Ви впевнені, що хочете вийти із системи?
        </ConfirmDialog>
      )}
    </div>
  )
}
