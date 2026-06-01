import React, { useState, useEffect } from 'react';
import {
  Drawer, Descriptions, Divider, Table, Button, Space, Typography,
  message, Modal, Tabs, Tag, Spin, Form, Input,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, EnvironmentOutlined } from '@ant-design/icons';
import api from '../../api/client';
import ServiceObjectForm from '../ServiceObjects/ServiceObjectForm';

const { Title, Text } = Typography;

interface ClientDetailsDrawerProps {
  clientId: string | null;
  isOpen: boolean;
  onClose: () => void;
  onRefresh: () => void;
}

const ClientDetailsDrawer: React.FC<ClientDetailsDrawerProps> = ({ clientId, isOpen, onClose, onRefresh }) => {
  const [client, setClient] = useState<any>(null);
  const [loadingClient, setLoadingClient] = useState(false);

  const [isObjectModalOpen, setIsObjectModalOpen] = useState(false);
  const [editingObject, setEditingObject] = useState<any>(null);

  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [editingContact, setEditingContact] = useState<any>(null);
  const [contactForm] = Form.useForm();
  const [savingContact, setSavingContact] = useState(false);

  const fetchClient = async (id: string) => {
    setLoadingClient(true);
    try {
      const res = await api.get(`/Clients/${id}`);
      setClient(res.data);
    } catch {
      message.error('Не удалось загрузить данные клиента');
    } finally {
      setLoadingClient(false);
    }
  };

  useEffect(() => {
    if (isOpen && clientId) {
      fetchClient(clientId);
    } else if (!isOpen) {
      setClient(null);
    }
  }, [isOpen, clientId]);

  const openAddContact = () => {
    setEditingContact(null);
    contactForm.resetFields();
    setContactModalOpen(true);
  };

  const openEditContact = (record: any) => {
    setEditingContact(record);
    contactForm.setFieldsValue({
      firstName: record.firstName,
      lastName: record.lastName,
      position: record.position,
      email: record.email,
      phoneNumber: record.phoneNumber,
    });
    setContactModalOpen(true);
  };

  const handleSaveContact = async () => {
    try {
      const values = await contactForm.validateFields();
      setSavingContact(true);
      if (editingContact) {
        await api.put(`/ContactPersons/${editingContact.id}`, {
          id: editingContact.id,
          ...values,
        });
        message.success('Контакт обновлён');
      } else {
        await api.post('/ContactPersons', {
          clientId,
          ...values,
        });
        message.success('Контакт добавлен');
      }
      setContactModalOpen(false);
      contactForm.resetFields();
      if (clientId) fetchClient(clientId);
      onRefresh();
    } catch (err: any) {
      const d = err?.response?.data;
      message.error(d?.detail || d?.title || 'Ошибка при сохранении');
    } finally {
      setSavingContact(false);
    }
  };

  const handleDeleteContact = (id: string) => {
    Modal.confirm({
      title: 'Удалить контактное лицо?',
      okText: 'Удалить',
      cancelText: 'Отмена',
      okButtonProps: { danger: true },
      onOk: async () => {
        try {
          await api.delete(`/ContactPersons/${id}`);
          message.success('Контактное лицо удалено');
          if (clientId) fetchClient(clientId);
          onRefresh();
        } catch {
          message.error('Не удалось удалить контакт');
        }
      },
    });
  };

  const handleDeleteObject = (id: string, name: string) => {
    Modal.confirm({
      title: `Удалить объект «${name}»?`,
      content: 'Объект будет помечен как удалённый.',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await api.delete(`/ServiceObjects/${id}`);
          message.success('Объект удалён');
          if (clientId) fetchClient(clientId);
          onRefresh();
        } catch {
          message.error('Не удалось удалить объект');
        }
      },
    });
  };

  const contactColumns = [
    { title: 'ФИО', render: (r: any) => `${r.firstName} ${r.lastName}` },
    { title: 'Должность', dataIndex: 'position', render: (v: string) => v || '—' },
    { title: 'Email', dataIndex: 'email', render: (v: string) => v || '—' },
    { title: 'Телефон', dataIndex: 'phoneNumber', render: (v: string) => v || '—' },
    {
      title: '',
      width: 80,
      render: (_: any, record: any) => (
        <Space size={4}>
          <Button size="small" type="text" icon={<EditOutlined />} onClick={() => openEditContact(record)} />
          <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => handleDeleteContact(record.id)} />
        </Space>
      ),
    },
  ];

  const objectColumns = [
    { title: 'Наименование', dataIndex: 'name' },
    { title: 'Адрес', dataIndex: 'address', render: (v: string) => v || '—' },
    {
      title: 'Координаты',
      render: (r: any) => r.latitude
        ? <Tag color="blue" icon={<EnvironmentOutlined />}>{Number(r.latitude).toFixed(4)}, {Number(r.longitude).toFixed(4)}</Tag>
        : <Tag color="default">Нет</Tag>,
    },
    {
      title: 'Статус',
      render: (r: any) => <Tag color={r.isActive ? 'green' : 'red'}>{r.isActive ? 'Активен' : 'Неактивен'}</Tag>,
    },
    {
      title: '',
      width: 80,
      render: (_: any, record: any) => (
        <Space size={4}>
          <Button size="small" type="text" icon={<EditOutlined />} onClick={() => { setEditingObject(record); setIsObjectModalOpen(true); }} />
          <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => handleDeleteObject(record.id, record.name)} />
        </Space>
      ),
    },
  ];

  const tabs = [
    {
      key: 'contacts',
      label: `Контактные лица (${client?.contactPersons?.length ?? 0})`,
      children: (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <Title level={5} style={{ margin: 0 }}>Контактные лица</Title>
            <Button type="dashed" icon={<PlusOutlined />} onClick={openAddContact}>
              Добавить контакт
            </Button>
          </div>
          <Table
            dataSource={client?.contactPersons ?? []}
            columns={contactColumns}
            rowKey="id"
            pagination={false}
            size="small"
            locale={{ emptyText: 'Нет контактных лиц' }}
          />
        </>
      ),
    },
    {
      key: 'objects',
      label: `Объекты (${client?.serviceObjects?.length ?? 0})`,
      children: (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <Title level={5} style={{ margin: 0 }}>Объекты обслуживания</Title>
            <Button
              type="dashed"
              icon={<PlusOutlined />}
              onClick={() => { setEditingObject(null); setIsObjectModalOpen(true); }}
            >
              Добавить объект
            </Button>
          </div>
          <Table
            dataSource={client?.serviceObjects ?? []}
            columns={objectColumns}
            rowKey="id"
            pagination={false}
            size="small"
            locale={{ emptyText: 'Нет привязанных объектов' }}
          />
        </>
      ),
    },
  ];

  return (
    <>
      <Drawer
        title={client ? `Клиент: ${client.name}` : 'Загрузка...'}
        placement="right"
        width={860}
        onClose={onClose}
        open={isOpen}
        extra={<Button onClick={onClose}>Закрыть</Button>}
      >
        {loadingClient ? (
          <div style={{ textAlign: 'center', paddingTop: 80 }}><Spin size="large" /></div>
        ) : client ? (
          <>
            <Descriptions bordered column={1} size="small">
              <Descriptions.Item label="Наименование">{client.name}</Descriptions.Item>
              <Descriptions.Item label="ИНН">{client.inn}</Descriptions.Item>
              <Descriptions.Item label="Email">{client.email || '—'}</Descriptions.Item>
              <Descriptions.Item label="Телефон">{client.phoneNumber || '—'}</Descriptions.Item>
              <Descriptions.Item label="Адрес">{client.address || '—'}</Descriptions.Item>
              <Descriptions.Item label="Статус">
                <Tag color={client.isActive ? 'green' : 'red'}>{client.isActive ? 'Активен' : 'Неактивен'}</Tag>
              </Descriptions.Item>
            </Descriptions>

            <Divider />

            <Tabs defaultActiveKey="contacts" items={tabs} />
          </>
        ) : (
          <Text type="secondary">Не удалось загрузить данные клиента</Text>
        )}
      </Drawer>

      {}
      <Modal
        title={editingContact ? 'Редактировать контакт' : 'Добавить контактное лицо'}
        open={contactModalOpen}
        onCancel={() => { setContactModalOpen(false); contactForm.resetFields(); }}
        onOk={handleSaveContact}
        okText={editingContact ? 'Сохранить' : 'Добавить'}
        cancelText="Отмена"
        confirmLoading={savingContact}
        width="min(500px, 95vw)"
      >
        <Form form={contactForm} layout="vertical" style={{ marginTop: 8 }}>
          <Space.Compact style={{ width: '100%', display: 'flex', gap: 8 }}>
            <Form.Item
              name="firstName"
              label="Имя"
              rules={[{ required: true, message: 'Введите имя' }]}
              style={{ flex: 1 }}
            >
              <Input placeholder="Алексей" />
            </Form.Item>
            <Form.Item
              name="lastName"
              label="Фамилия"
              rules={[{ required: true, message: 'Введите фамилию' }]}
              style={{ flex: 1 }}
            >
              <Input placeholder="Иванов" />
            </Form.Item>
          </Space.Compact>
          <Form.Item name="position" label="Должность">
            <Input placeholder="Главный инженер" />
          </Form.Item>
          <Form.Item
            name="email"
            label="Email"
            rules={[{ type: 'email', message: 'Некорректный формат email' }]}
          >
            <Input placeholder="ivanov@example.com" />
          </Form.Item>
          <Form.Item name="phoneNumber" label="Телефон">
            <Input placeholder="+7 (777) 000-00-00" />
          </Form.Item>
        </Form>
      </Modal>

      {}
      <ServiceObjectForm
        isOpen={isObjectModalOpen}
        onClose={() => setIsObjectModalOpen(false)}
        onSuccess={() => {
          setIsObjectModalOpen(false);
          if (clientId) fetchClient(clientId);
          onRefresh();
        }}
        initialData={editingObject
          ? { ...editingObject, clientId: client?.id }
          : { clientId: client?.id }
        }
      />
    </>
  );
};

export default ClientDetailsDrawer;
