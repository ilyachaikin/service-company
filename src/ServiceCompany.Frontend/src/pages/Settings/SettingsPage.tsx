import React, { useState } from 'react';
import { Card, Form, Input, Button, message, Typography, Divider, Switch, Alert, Space, Row, Col } from 'antd';
import { LockOutlined, CheckCircleOutlined, BulbOutlined, MoonOutlined, SunOutlined } from '@ant-design/icons';
import api from '../../api/client';
import { useTheme } from '../../store/ThemeContext';

const { Title, Text } = Typography;

const SettingsPage: React.FC = () => {
  const [passwordForm] = Form.useForm();
  const [loadingPassword, setLoadingPassword] = useState(false);
  const [passwordChanged, setPasswordChanged] = useState(false);
  const { setTheme, isDark } = useTheme();

  const handleChangePassword = async (values: any) => {
    setLoadingPassword(true);
    setPasswordChanged(false);
    try {
      await api.post('/auth/change-password', {
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      message.success('Пароль успешно изменён');
      passwordForm.resetFields();
      setPasswordChanged(true);
    } catch (err: any) {
      const errText = err.response?.data;
      message.error(typeof errText === 'string' ? errText : 'Проверьте введённые данные');
    } finally {
      setLoadingPassword(false);
    }
  };

  return (
    <div style={{ maxWidth: 700, margin: '0 auto' }}>
      <Title level={2}>Настройки</Title>

      {}
      <Card
        title={
          <Space>
            <BulbOutlined style={{ color: '#1890ff' }} />
            Оформление
          </Space>
        }
        style={{ marginBottom: 24 }}
      >
        <Text type="secondary" style={{ display: 'block', marginBottom: 20 }}>
          Выберите тему интерфейса. Настройка сохраняется в браузере.
        </Text>

        <Row gutter={16}>
          <Col>
            <Card
              hoverable
              style={{
                width: 160,
                cursor: 'pointer',
                borderColor: !isDark ? '#1890ff' : undefined,
                borderWidth: !isDark ? 2 : 1,
                background: '#ffffff',
              }}
              bodyStyle={{ padding: 16, textAlign: 'center' }}
              onClick={() => setTheme('light')}
            >
              <SunOutlined style={{ fontSize: 28, color: '#faad14', display: 'block', marginBottom: 8 }} />
              <Text strong style={{ color: '#000' }}>Светлая</Text>
              {!isDark && <div style={{ marginTop: 6 }}><Text type="success" style={{ fontSize: 12 }}>✓ Активна</Text></div>}
            </Card>
          </Col>
          <Col>
            <Card
              hoverable
              style={{
                width: 160,
                cursor: 'pointer',
                borderColor: isDark ? '#1890ff' : undefined,
                borderWidth: isDark ? 2 : 1,
                background: '#141414',
              }}
              bodyStyle={{ padding: 16, textAlign: 'center' }}
              onClick={() => setTheme('dark')}
            >
              <MoonOutlined style={{ fontSize: 28, color: '#adb5bd', display: 'block', marginBottom: 8 }} />
              <Text strong style={{ color: '#fff' }}>Тёмная</Text>
              {isDark && <div style={{ marginTop: 6 }}><Text style={{ fontSize: 12, color: '#52c41a' }}>✓ Активна</Text></div>}
            </Card>
          </Col>
        </Row>

        <Divider />
        <Space align="center">
          <Text>Быстрое переключение:</Text>
          <Switch
            checked={isDark}
            onChange={(checked) => setTheme(checked ? 'dark' : 'light')}
            checkedChildren={<MoonOutlined />}
            unCheckedChildren={<SunOutlined />}
          />
          <Text type="secondary">{isDark ? 'Тёмная тема' : 'Светлая тема'}</Text>
        </Space>
      </Card>

      {}
      <Card
        title={
          <Space>
            <LockOutlined style={{ color: '#1890ff' }} />
            Безопасность
          </Space>
        }
        style={{ marginBottom: 24 }}
      >
        <Text type="secondary" style={{ display: 'block', marginBottom: 20 }}>
          Регулярно меняйте пароль для обеспечения безопасности учётной записи.
        </Text>

        {passwordChanged && (
          <Alert
            message="Пароль успешно изменён"
            type="success"
            icon={<CheckCircleOutlined />}
            showIcon
            style={{ marginBottom: 20 }}
            closable
            onClose={() => setPasswordChanged(false)}
          />
        )}

        <Form
          form={passwordForm}
          layout="vertical"
          onFinish={handleChangePassword}
          style={{ maxWidth: 450 }}
        >
          <Form.Item
            name="currentPassword"
            label="Текущий пароль"
            rules={[{ required: true, message: 'Введите текущий пароль' }]}
          >
            <Input.Password placeholder="Текущий пароль" />
          </Form.Item>

          <Form.Item
            name="newPassword"
            label="Новый пароль"
            rules={[
              { required: true, message: 'Введите новый пароль' },
              { min: 8, message: 'Минимум 8 символов' },
              { pattern: /(?=.*[A-Z])(?=.*\d)/, message: 'Нужна заглавная буква и цифра' },
            ]}
          >
            <Input.Password placeholder="Новый пароль" />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label="Подтверждение пароля"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: 'Подтвердите новый пароль' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) return Promise.resolve();
                  return Promise.reject(new Error('Пароли не совпадают'));
                },
              }),
            ]}
          >
            <Input.Password placeholder="Повторите новый пароль" />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loadingPassword}>
              Изменить пароль
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default SettingsPage;
