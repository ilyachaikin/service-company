import React, { useState, useEffect } from 'react';
import { Table, Button, Input, Space, Tag, Modal, message, Card, Typography } from 'antd';
import { PlusOutlined, SearchOutlined, EditOutlined, DeleteOutlined, InfoCircleOutlined } from '@ant-design/icons';
import api from '../../api/client';
import ClientForm from './ClientForm';
import ClientDetailsDrawer from './ClientDetailsDrawer';

const { Title } = Typography;

const ClientsPage: React.FC = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState({ page: 1, pageSize: 10, searchTerm: '' });
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  const [editingClient, setEditingClient] = useState<any>(null);

  const fetchClients = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Clients', { params });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch (err) {
      message.error('Не удалось загрузить список клиентов');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchClients(); }, [params]);

  const handleDelete = (id: string) => {
    Modal.confirm({
      title: 'Вы уверены?',
      content: 'Клиент и все связанные данные будут деактивированы.',
      okText: 'Удалить',
      cancelText: 'Отмена',
      okButtonProps: { danger: true },
      onOk: async () => {
        await api.delete(`/Clients/${id}`);
        message.success('Клиент удалён');
        fetchClients();
      }
    });
  };

  const columns = [
    { title: 'Наименование', dataIndex: 'name', key: 'name', sorter: true },
    { title: 'ИНН', dataIndex: 'inn', key: 'inn' },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Телефон', dataIndex: 'phoneNumber', key: 'phoneNumber' },
    {
      title: 'Статус',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (active: boolean) => (
        <Tag color={active ? 'green' : 'red'}>{active ? 'Активен' : 'Неактивен'}</Tag>
      )
    },
    {
      title: 'Действия',
      key: 'action',
      render: (_: any, record: any) => (
        <Space size="middle">
          <Button icon={<InfoCircleOutlined />} onClick={() => { setSelectedClientId(record.id); setIsDrawerOpen(true); }} />
          <Button icon={<EditOutlined />} onClick={() => { setEditingClient(record); setIsModalOpen(true); }} />
          <Button icon={<DeleteOutlined />} danger onClick={() => handleDelete(record.id)} />
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Клиенты</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingClient(null); setIsModalOpen(true); }}>
          Добавить клиента
        </Button>
      </div>

      <Card>
        <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Input
            placeholder="Поиск по наименованию или ИНН"
            prefix={<SearchOutlined />}
            style={{ width: 300, minWidth: 180 }}
            onChange={(e) => setParams({ ...params, searchTerm: e.target.value, page: 1 })}
          />
        </div>

        <Table
          columns={columns}
          dataSource={data}
          loading={loading}
          rowKey="id"
          scroll={{ x: 700 }}
          pagination={{
            current: params.page,
            pageSize: params.pageSize,
            total: total,
            onChange: (page, pageSize) => setParams({ ...params, page, pageSize })
          }}
        />
      </Card>

      <ClientForm
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={() => { setIsModalOpen(false); fetchClients(); }}
        initialData={editingClient}
      />
      <ClientDetailsDrawer
        clientId={selectedClientId}
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        onRefresh={fetchClients}
      />
    </div>
  );
};

export default ClientsPage;
