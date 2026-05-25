import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '../../widgets/app-shell/AppShell'
import { useAuth } from '../../features/auth/model/useAuth'
import { CommissionsPage } from '../../pages/commissions/CommissionsPage'
import { GeneratorRedirectPage } from '../../pages/generator/GeneratorRedirectPage'
import { GroupsPage } from '../../pages/groups/GroupsPage'
import { LoginPage } from '../../pages/login/LoginPage'

function ProtectedShell() {
  const { isAuthenticated } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <AppShell />
}

function HomeRedirect() {
  const { isAuthenticated } = useAuth()

  return <Navigate to={isAuthenticated ? '/groups' : '/login'} replace />
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/" element={<HomeRedirect />} />
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedShell />}>
        <Route path="/groups" element={<GroupsPage />} />
        <Route path="/groups/:defenseYear" element={<GroupsPage />} />
        <Route path="/groups/:defenseYear/commission" element={<GroupsPage />} />
        <Route path="/groups/:defenseYear/:groupId" element={<GroupsPage />} />
        <Route path="/groups/:defenseYear/:groupId/:view" element={<GroupsPage />} />
        <Route path="/groups/:defenseYear/:groupId/students/:studentId" element={<GroupsPage />} />
        <Route path="/commissions" element={<CommissionsPage />} />
        <Route path="/commissions/:defenseYear" element={<CommissionsPage />} />
        <Route path="/commissions/:defenseYear/:commissionId" element={<CommissionsPage />} />
        <Route path="/generator" element={<GeneratorRedirectPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
