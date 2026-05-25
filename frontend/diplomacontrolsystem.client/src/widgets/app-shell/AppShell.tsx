import { LogOut, UserRoundCog } from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/model/useAuth'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'

const navItems = [
  { to: '/groups', label: 'Групи' },
  { to: '/generator', label: 'Генератор' },
]

export function AppShell() {
  const { logout, secretaryEmail } = useAuth()
  const navigate = useNavigate()
  const [isLogoutOpen, setIsLogoutOpen] = useState(false)

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

        <div className="flex items-center gap-5">
          <div
            title={secretaryEmail}
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
