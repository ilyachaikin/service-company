import React, { useState } from 'react';
import { Form, Input, Button, Card, Typography, message, Layout, Divider } from 'antd';
import { UserOutlined, LockOutlined, ThunderboltOutlined } from '@ant-design/icons';
import { useAuth } from '../store/AuthContext';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;

const DEMO_CREDENTIALS = {
  email: 'admin@servicecompany.com',
  password: 'Admin123!',
};

const LoginPage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [form] = Form.useForm();
  const { login } = useAuth();
  const navigate = useNavigate();

  const onFinish = async (values: any) => {
    setLoading(true);
    try {
      await login(values.email, values.password);
      message.success('Добро пожаловать!');
      navigate('/');
    } catch (error: any) {
      const data = error.response?.data;
      const text = typeof data === 'string'
        ? data
        : data?.detail || data?.title || 'Неверный логин или пароль';
      message.error(text);
    } finally {
      setLoading(false);
    }
  };

  const fillDemo = () => {
    form.setFieldsValue(DEMO_CREDENTIALS);
  };

  return (
    <Layout style={{ minHeight: '100vh', background: 'linear-gradient(135deg, #4F46E5 0%, #1E293B 100%)', display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '16px' }}>
      <Card style={{ width: '100%', maxWidth: 400, borderRadius: 12, boxShadow: '0 10px 25px rgba(0,0,0,0.2)' }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Title level={2} style={{ color: '#4F46E5', marginBottom: 0 }}>ServiceCompany</Title>
          <Text type="secondary">Система управления сервисным обслуживанием</Text>
        </div>

        <Form form={form} name="login" onFinish={onFinish} layout="vertical" size="large">
          <Form.Item name="email" rules={[{ required: true, message: 'Введите email!' }, { type: 'email', message: 'Неверный формат email' }]}>
            <Input prefix={<UserOutlined />} placeholder="Email" />
          </Form.Item>

          <Form.Item name="password" rules={[{ required: true, message: 'Введите пароль!' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Пароль" />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading} block style={{ height: 45, fontSize: 16 }}>
              Войти
            </Button>
          </Form.Item>
        </Form>

        <Divider plain style={{ margin: '0 0 16px' }}>
          <Text type="secondary" style={{ fontSize: 12 }}>демо-доступ</Text>
        </Divider>

        <Button
          block
          icon={<ThunderboltOutlined />}
          onClick={fillDemo}
          style={{ color: '#4F46E5', borderColor: '#4F46E5' }}
        >
          Войти как Администратор
        </Button>
      </Card>
    </Layout>
  );
};

export default LoginPage;
