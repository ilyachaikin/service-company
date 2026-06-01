import React from 'react';
import { Card, Typography, Avatar, Descriptions, Tag, Button, Row, Col, Space } from 'antd';
import { UserOutlined, MailOutlined, SafetyCertificateOutlined, SettingOutlined } from '@ant-design/icons';
import { useAuth } from '../../store/AuthContext';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;

const roleLabels: Record<string, { label: string; color: string }> = {
  Admin: { label: 'Администратор', color: 'red' },
  Manager: { label: 'Менеджер', color: 'blue' },
  Engineer: { label: 'Инженер', color: 'green' },
  Dispatcher: { label: 'Диспетчер', color: 'orange' },
};

const ProfilePage: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  if (!user) return null;

  const initials = user.fullName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  return (
    <div style={{ maxWidth: 700, margin: '0 auto' }}>
      <Title level={2}>Профиль</Title>

      <Card style={{ marginBottom: 24 }}>
        <Row gutter={[24, 24]} align="middle">
          <Col>
            <Avatar
              size={80}
              style={{ backgroundColor: '#1890ff', fontSize: 28, fontWeight: 600 }}
            >
              {initials}
            </Avatar>
          </Col>
          <Col flex={1}>
            <Title level={3} style={{ margin: 0 }}>{user.fullName}</Title>
            <Space style={{ marginTop: 4 }}>
              {user.roles.map((role) => {
                const info = roleLabels[role] || { label: role, color: 'default' };
                return (
                  <Tag key={role} color={info.color} icon={<SafetyCertificateOutlined />}>
                    {info.label}
                  </Tag>
                );
              })}
            </Space>
          </Col>
        </Row>
      </Card>

      <Card title="Данные учётной записи" style={{ marginBottom: 24 }}>
        <Descriptions column={1} bordered>
          <Descriptions.Item label={<><UserOutlined style={{ marginRight: 6 }} />ФИО</>}>
            {user.fullName}
          </Descriptions.Item>
          <Descriptions.Item label={<><MailOutlined style={{ marginRight: 6 }} />Email</>}>
            {user.email}
          </Descriptions.Item>
          <Descriptions.Item label="Имя пользователя">
            {user.userName}
          </Descriptions.Item>
          <Descriptions.Item label="Роли">
            <Space>
              {user.roles.map((role) => {
                const info = roleLabels[role] || { label: role, color: 'default' };
                return <Tag key={role} color={info.color}>{info.label}</Tag>;
              })}
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label="ID пользователя">
            <Text type="secondary" style={{ fontFamily: 'monospace', fontSize: 12 }}>{user.id}</Text>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card>
        <Space>
          <Button
            type="primary"
            icon={<SettingOutlined />}
            onClick={() => navigate('/settings')}
          >
            Изменить пароль
          </Button>
          <Button danger onClick={logout}>
            Выйти из системы
          </Button>
        </Space>
      </Card>
    </div>
  );
};

export default ProfilePage;
