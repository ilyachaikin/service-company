import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { ConfigProvider, theme as antdTheme } from 'antd';
import ru_RU from 'antd/locale/ru_RU';
import { AuthProvider, useAuth } from './store/AuthContext';
import { ThemeProvider, useTheme } from './store/ThemeContext';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import MainLayout from './layouts/MainLayout';
import DashboardPage from './pages/DashboardPage';
import ClientsPage from './pages/Clients/ClientsPage';
import ServiceObjectsPage from './pages/ServiceObjects/ServiceObjectsPage';
import EquipmentsPage from './pages/Equipments/EquipmentsPage';
import TicketsPage from './pages/Tickets/TicketsPage';
import TicketCreatePage from './pages/Tickets/TicketCreatePage';
import TicketDetailsPage from './pages/Tickets/TicketDetailsPage';
import ContractsPage from './pages/Contracts/ContractsPage';
import InvoicesPage from './pages/Invoices/InvoicesPage';
import SlaPoliciesPage from './pages/SlaPolicies/SlaPoliciesPage';
import SettingsPage from './pages/Settings/SettingsPage';
import ProfilePage from './pages/Profile/ProfilePage';
import UsersPage from './pages/Users/UsersPage';
import MapPage from './pages/Map/MapPage';
import MaintenancePlansPage from './pages/MaintenancePlans/MaintenancePlansPage';
import ReportsPage from './pages/Reports/ReportsPage';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import 'dayjs/locale/ru';

dayjs.extend(relativeTime);
dayjs.locale('ru');

const RoleBasedHome: React.FC = () => {
  const { user } = useAuth();
  const roles = user?.roles ?? [];
  const isEngineerOnly = roles.includes('Engineer')
    && !roles.includes('Admin')
    && !roles.includes('Manager')
    && !roles.includes('Accountant');
  if (isEngineerOnly) return <Navigate to="/tickets" replace />;
  return <DashboardPage />;
};

const ThemedApp: React.FC = () => {
  const { isDark } = useTheme();

  return (
    <ConfigProvider
      locale={ru_RU}
      theme={{
        algorithm: isDark ? antdTheme.darkAlgorithm : antdTheme.defaultAlgorithm,
        token: {
          colorPrimary: '#4F46E5',
          borderRadius: 3,
          fontFamily: "'IBM Plex Sans', system-ui, -apple-system, sans-serif",
        },
        components: {
          Layout: {
            siderBg: '#1E293B',
          },
          Menu: {
            darkItemBg: '#1E293B',
            darkSubMenuItemBg: '#162032',
            darkPopupBg: '#1E293B',
          },
        },
      }}
    >
      <AuthProvider>
        <Router>
          <Routes>
            <Route path="/login" element={<LoginPage />} />

            <Route element={<ProtectedRoute />}>
              <Route element={<MainLayout />}>
                <Route path="/" element={<RoleBasedHome />} />
                <Route path="/clients" element={<ClientsPage />} />
                <Route path="/tickets" element={<TicketsPage />} />
                <Route path="/tickets/create" element={<TicketCreatePage />} />
                <Route path="/tickets/:id" element={<TicketDetailsPage />} />
                <Route path="/equipment" element={<EquipmentsPage />} />
                <Route path="/objects" element={<ServiceObjectsPage />} />
                <Route path="/contracts" element={<ContractsPage />} />
                <Route path="/invoices" element={<InvoicesPage />} />
                <Route path="/sla-policies" element={<SlaPoliciesPage />} />
                <Route path="/map" element={<MapPage />} />
                <Route path="/maintenance-plans" element={<MaintenancePlansPage />} />
                <Route path="/reports" element={<ReportsPage />} />
                <Route path="/users" element={<UsersPage />} />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="/profile" element={<ProfilePage />} />
              </Route>
            </Route>

            <Route path="/unauthorized" element={<div>Нет доступа</div>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Router>
      </AuthProvider>
    </ConfigProvider>
  );
};

const App: React.FC = () => (
  <ThemeProvider>
    <ThemedApp />
  </ThemeProvider>
);

export default App;
