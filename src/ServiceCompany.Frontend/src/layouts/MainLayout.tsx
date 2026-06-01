import React, { useState } from 'react';
import { Layout, Menu, Button, Space, Avatar, Dropdown, Typography, Tooltip, Switch, Drawer, Grid } from 'antd';
import {
  DashboardOutlined,
  TeamOutlined,
  ToolOutlined,
  FileTextOutlined,
  LogoutOutlined,
  UserOutlined,
  MenuUnfoldOutlined,
  MenuFoldOutlined,
  SettingOutlined,
  EnvironmentOutlined,
  AuditOutlined,
  DollarOutlined,
  SafetyCertificateOutlined,
  GlobalOutlined,
  UsergroupAddOutlined,
  MoonOutlined,
  SunOutlined,
  ScheduleOutlined,
  BarChartOutlined,
  MenuOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';
import { useTheme } from '../store/ThemeContext';

const { Header, Sider, Content } = Layout;
const { Title } = Typography;
const { useBreakpoint } = Grid;

const ALL_MENU_ITEMS = [
  { key: '/',                  icon: <DashboardOutlined />,         label: 'Главная',       roles: ['Admin', 'Manager', 'Accountant'] },
  { key: '/clients',           icon: <TeamOutlined />,              label: 'Клиенты',       roles: ['Admin', 'Manager'] },
  { key: '/contracts',         icon: <AuditOutlined />,             label: 'Договоры',      roles: ['Admin', 'Manager', 'Accountant'] },
  { key: '/sla-policies',      icon: <SafetyCertificateOutlined />, label: 'SLA-политики',  roles: ['Admin', 'Manager'] },
  { key: '/invoices',          icon: <DollarOutlined />,            label: 'Счета',         roles: ['Admin', 'Manager', 'Accountant'] },
  { key: '/objects',           icon: <EnvironmentOutlined />,       label: 'Объекты',       roles: ['Admin', 'Manager'] },
  { key: '/equipment',         icon: <ToolOutlined />,              label: 'Оборудование',  roles: ['Admin', 'Manager'] },
  { key: '/tickets',           icon: <FileTextOutlined />,          label: 'Заявки',        roles: ['Admin', 'Manager', 'Engineer'] },
  { key: '/maintenance-plans', icon: <ScheduleOutlined />,          label: 'Планы ТО',      roles: ['Admin', 'Manager'] },
  { key: '/map',               icon: <GlobalOutlined />,            label: 'Карта',         roles: ['Admin', 'Manager', 'Engineer'] },
  { key: '/reports',           icon: <BarChartOutlined />,          label: 'Отчёты',        roles: ['Admin', 'Manager', 'Accountant'] },
  { key: '/users',             icon: <UsergroupAddOutlined />,      label: 'Пользователи',  roles: ['Admin'] },
  { key: '/settings',          icon: <SettingOutlined />,           label: 'Настройки',     roles: ['Admin', 'Manager', 'Engineer', 'Accountant'] },
];

const MainLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const { user, logout } = useAuth();
  const { isDark, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const location = useLocation();
  const screens = useBreakpoint();

  const isMobile = !screens.md;

  const userRoles: string[] = user?.roles ?? [];

  const menuItems = ALL_MENU_ITEMS
    .filter(item => item.roles.some(r => userRoles.includes(r)))
    .map(({ key, icon, label }) => ({ key, icon, label }));

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Профиль',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: 'Настройки',
      onClick: () => navigate('/settings'),
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Выйти',
      onClick: logout,
    },
  ];

  const headerBg = isDark ? '#1f1f1f' : '#ffffff';
  const headerShadow = isDark
    ? '0 1px 4px rgba(0,0,0,0.4)'
    : '0 1px 4px rgba(0,21,41,.08)';

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key);
    if (isMobile) setMobileMenuOpen(false);
  };

  const siderLogo = (
    <div style={{
      height: 64,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: 16,
      borderBottom: '1px solid rgba(255,255,255,0.1)',
      flexShrink: 0,
    }}>
      <Title level={4} style={{ color: '#fff', margin: 0, whiteSpace: 'nowrap' }}>
        {collapsed && !isMobile ? 'SC' : 'ServiceCompany'}
      </Title>
    </div>
  );

  const siderMenu = (
    <Menu
      theme="dark"
      mode="inline"
      selectedKeys={[location.pathname]}
      items={menuItems}
      onClick={handleMenuClick}
      style={{ flex: 1, overflow: 'auto' }}
    />
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {}
      {!isMobile && (
        <Sider
          trigger={null}
          collapsible
          collapsed={collapsed}
          theme="dark"
          width={220}
          style={{ position: 'fixed', left: 0, top: 0, bottom: 0, zIndex: 100, overflow: 'hidden' }}
        >
          {siderLogo}
          {siderMenu}
        </Sider>
      )}

      {}
      <Drawer
        placement="left"
        open={mobileMenuOpen}
        onClose={() => setMobileMenuOpen(false)}
        width={220}
        styles={{
          body: { padding: 0, background: '#1E293B', display: 'flex', flexDirection: 'column' },
          header: { display: 'none' },
          wrapper: { maxWidth: '80vw' },
        }}
        style={{ padding: 0 }}
      >
        {siderLogo}
        {siderMenu}
      </Drawer>

      <Layout style={{ marginLeft: isMobile ? 0 : (collapsed ? 80 : 220), transition: 'margin-left 0.2s' }}>
        <Header style={{
          padding: '0 16px',
          background: headerBg,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          boxShadow: headerShadow,
          position: 'sticky',
          top: 0,
          zIndex: 99,
        }}>
          {}
          {isMobile ? (
            <Button
              type="text"
              icon={<MenuOutlined />}
              onClick={() => setMobileMenuOpen(true)}
              style={{ fontSize: 18, width: 48, height: 48 }}
            />
          ) : (
            <Button
              type="text"
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => setCollapsed(!collapsed)}
              style={{ fontSize: 16, width: 64, height: 64 }}
            />
          )}

          {}
          <Space size={isMobile ? 8 : 'middle'}>
            <Tooltip title={isDark ? 'Светлая тема' : 'Тёмная тема'}>
              <Switch
                checked={isDark}
                onChange={toggleTheme}
                checkedChildren={<MoonOutlined />}
                unCheckedChildren={<SunOutlined />}
              />
            </Tooltip>

            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
              <Space style={{ cursor: 'pointer' }}>
                <Avatar style={{ backgroundColor: '#1890ff' }} icon={<UserOutlined />} />
                {!isMobile && <span>{user?.fullName}</span>}
              </Space>
            </Dropdown>
          </Space>
        </Header>

        <Content
          className="main-content"
          style={{
            margin: isMobile ? '12px 8px' : '24px 16px',
            padding: isMobile ? 16 : 24,
            minHeight: 280,
            background: isDark ? '#141414' : '#ffffff',
            borderRadius: 8,
            overflow: 'initial',
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
};

export default MainLayout;
