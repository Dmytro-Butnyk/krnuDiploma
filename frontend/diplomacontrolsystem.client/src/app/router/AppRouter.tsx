import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '../../widgets/app-shell/AppShell'
import { useAuth } from '../../features/auth/model/useAuth'
import { TemplatesPage } from '../../document-generator/pages/templates/TemplatesPage'
import { CommissionsPage } from '../../pages/commissions/CommissionsPage'
import { GroupsPage } from '../../pages/groups/GroupsPage'
import { LoginPage } from '../../pages/login/LoginPage'
import { ManagementPage } from '../../pages/management/ManagementPage'

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
        <Route path="/management" element={<ManagementPage />} />
        <Route path="/generator" element={<Navigate to="/document-generator/templates" replace />} />
        <Route path="/document-generator" element={<Navigate to="/document-generator/templates" replace />} />
        <Route path="/document-generator/templates" element={<TemplatesPage />} />
        <Route path="/document-generator/templates/new" element={<TemplatesPage routeMode="upload" />} />
        <Route path="/document-generator/templates/new/constructor" element={<TemplatesPage routeMode="create-constructor" />} />
        <Route path="/document-generator/templates/:templateId" element={<TemplatesPage routeMode="details" />} />
        <Route path="/document-generator/templates/:templateId/edit" element={<TemplatesPage routeMode="edit" />} />
        <Route path="/document-generator/templates/:templateId/generate" element={<TemplatesPage routeMode="generate" />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
