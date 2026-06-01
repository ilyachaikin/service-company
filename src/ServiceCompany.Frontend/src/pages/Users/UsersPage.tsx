import React, { useState, useEffect } from 'react';
import {
  Table, Button, Modal, Form, Input, Select, Tag, Switch, Space,
  message, Card, Typography, Tooltip, Avatar,
} from 'antd';
import {
  PlusOutlined, EditOutlined, KeyOutlined, UserOutlined,
  CheckCircleOutlined, StopOutlined,
} from '@ant-design/icons';
import api from '../../api/client';

const { Title, Text } = Typography;

const ROLES = ['Admin', 'Manager', 'Engineer', 'Accountant'];

const roleColors: Record<string, string> = {
  Admin:      'red',
  Manager:    'blue',
  Engineer:   'green',
  Accountant: 'orange',
};

const roleLabels: Record<string, string> = {
  Admin:      'Администратор',
  Manager:    'Менеджер',
  Engineer:   'Инженер',
  Accountant: 'Бухгалтер',
};

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isPasswordOpen, setIsPasswordOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<any>(null);

  const [createForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const [passwordForm] = Form.useForm();

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Users');
      setUsers(res.data);
    } catch (err: any) {
      message.error('Не удалось загрузить пользователей');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchUsers(); }, []);

  const handleCreate = async (values: any) => {
    try {
      await api.post('/Users', values);
      message.success('Пользователь создан');
      setIsCreateOpen(false);
      createForm.resetFields();
      fetchUsers();
    } catch (err: any) {
      message.error(err.response?.data || 'Ошибка при создании');
    }
  };

  const openEdit = (user: any) => {
    setSelectedUser(user);
    editForm.setFieldsValue({
      fullName: user.fullName,
      isActive: user.isActive ?? true,
      role: user.roles?.[0] ?? 'Engineer',
    });
    setIsEditOpen(true);
  };

  const handleEdit = async (values: any) => {
    try {
      await Promise.all([
        api.put(`/Users/${selectedUser.id}`, { fullName: values.fullName, isActive: values.isActive }),
        api.put(`/Users/${selectedUser.id}/role`, { role: values.role }),
      ]);
      message.success('Данные пользователя обновлены');
      setIsEditOpen(false);
      fetchUsers();
    } catch (err: any) {
      message.error(err.response?.data || 'Ошибка при обновлении');
    }
  };

  const openPasswordReset = (user: any) => {
    setSelectedUser(user);
    passwordForm.resetFields();
    setIsPasswordOpen(true);
  };

  const handlePasswordReset = async (values: any) => {
    try {
      await api.post(`/Users/${selectedUser.id}/reset-password`, { newPassword: values.newPassword });
      message.success('Пароль успешно сброшен');
      setIsPasswordOpen(false);
    } catch (err: any) {
      message.error(err.response?.data || 'Ошибка при сбросе пароля');
    }
  };

  const columns = [
    {
      title: 'Пользователь',
      key: 'user',
      render: (_: any, record: any) => (
        <Space>
          <Avatar style={{ backgroundColor: roleColors[record.roles?.[0]] || '#ccc' }}>
            {record.fullName?.charAt(0)?.toUpperCase() || <UserOutlined />}
          </Avatar>
          <div>
            <div style={{ fontWeight: 600 }}>{record.fullName}</div>
            <Text type="secondary" style={{ fontSize: 12 }}>{record.email}</Text>
          </div>
        </Space>
      ),
    },
    {
      title: 'Роль',
      key: 'roles',
      render: (_: any, record: any) =>
        record.roles?.map((r: string) => (
          <Tag key={r} color={roleColors[r] || 'default'}>{roleLabels[r] || r}</Tag>
        )),
    },
    {
      title: 'Статус',
      key: 'isActive',
      render: (_: any, record: any) =>
        record.isActive !== false
          ? <Tag icon={<CheckCircleOutlined />} color="success">Активен</Tag>
          : <Tag icon={<StopOutlined />} color="error">Заблокирован</Tag>,
    },
    {
      title: 'Действия',
      key: 'actions',
      render: (_: any, record: any) => (
        <Space>
          <Tooltip title="Редактировать">
            <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(record)} />
          </Tooltip>
          <Tooltip title="Сбросить пароль">
            <Button type="text" icon={<KeyOutlined />} onClick={() => openPasswordReset(record)} />
          </Tooltip>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Управление пользователями</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { createForm.resetFields(); setIsCreateOpen(true); }}>
          Добавить пользователя
        </Button>
      </div>

      <Card>
        <Table
          columns={columns}
          dataSource={users}
          loading={loading}
          rowKey="id"
          pagination={{ pageSize: 20 }}
          scroll={{ x: 600 }}
        />
      </Card>

      {}
      <Modal
        title="Новый пользователь"
        open={isCreateOpen}
        onCancel={() => setIsCreateOpen(false)}
        onOk={() => createForm.submit()}
        okText="Создать"
        cancelText="Отмена"
        width="min(520px, 95vw)"
      >
        <Form form={createForm} layout="vertical" onFinish={handleCreate}>
          <Form.Item name="fullName" label="ФИО" rules={[{ required: true, message: 'Введите ФИО' }]}>
            <Input placeholder="Иванов Иван Иванович" />
          </Form.Item>
          <Form.Item
            name="email"
            label="Email"
            rules={[{ required: true, message: 'Введите email' }, { type: 'email', message: 'Неверный формат' }]}
          >
            <Input placeholder="user@company.kz" />
          </Form.Item>
          <Form.Item
            name="password"
            label="Пароль"
            rules={[
              { required: true, message: 'Введите пароль' },
              { min: 8, message: 'Минимум 8 символов' },
              { pattern: /(?=.*[A-Z])(?=.*\d)/, message: 'Нужна заглавная буква и цифра' },
            ]}
          >
            <Input.Password placeholder="Минимум 8 символов" />
          </Form.Item>
          <Form.Item name="role" label="Роль" rules={[{ required: true, message: 'Выберите роль' }]} initialValue="Engineer">
            <Select>
              {ROLES.map(r => (
                <Select.Option key={r} value={r}>
                  <Tag color={roleColors[r]}>{roleLabels[r]}</Tag>
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>

      {}
      <Modal
        title={`Редактировать: ${selectedUser?.fullName}`}
        open={isEditOpen}
        onCancel={() => setIsEditOpen(false)}
        onOk={() => editForm.submit()}
        okText="Сохранить"
        cancelText="Отмена"
        width="min(520px, 95vw)"
      >
        <Form form={editForm} layout="vertical" onFinish={handleEdit}>
          <Form.Item name="fullName" label="ФИО" rules={[{ required: true, message: 'Введите ФИО' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="role" label="Роль" rules={[{ required: true }]}>
            <Select>
              {ROLES.map(r => (
                <Select.Option key={r} value={r}>
                  <Tag color={roleColors[r]}>{roleLabels[r]}</Tag>
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="isActive" label="Активен" valuePropName="checked">
            <Switch checkedChildren="Да" unCheckedChildren="Нет" />
          </Form.Item>
        </Form>
      </Modal>

      {}
      <Modal
        title={`Сброс пароля: ${selectedUser?.fullName}`}
        open={isPasswordOpen}
        onCancel={() => setIsPasswordOpen(false)}
        onOk={() => passwordForm.submit()}
        okText="Сбросить"
        okButtonProps={{ danger: true }}
        cancelText="Отмена"
        width="min(460px, 95vw)"
      >
        <Form form={passwordForm} layout="vertical" onFinish={handlePasswordReset}>
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
            label="Подтверждение"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: 'Подтвердите пароль' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) return Promise.resolve();
                  return Promise.reject(new Error('Пароли не совпадают'));
                },
              }),
            ]}
          >
            <Input.Password placeholder="Повторите пароль" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default UsersPage;
