import React, { useState, useEffect } from 'react';
import { Table, Button, Input, Space, Tag, message, Card, Typography, Modal } from 'antd';
import { PlusOutlined, SearchOutlined, EditOutlined, DeleteOutlined, EnvironmentOutlined } from '@ant-design/icons';
import api from '../../api/client';
import ServiceObjectForm from './ServiceObjectForm';

const { Title } = Typography;

const ServiceObjectsPage: React.FC = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState({ page: 1, pageSize: 10, searchTerm: '', clientId: undefined });
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingObject, setEditingObject] = useState<any>(null);

  const fetchObjects = async () => {
    setLoading(true);
    try {
      const res = await api.get('/ServiceObjects', { params });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch (err) {
      message.error('Не удалось загрузить объекты');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchObjects(); }, [params]);

  const handleDelete = (id: string, name: string) => {
    Modal.confirm({
      title: `Удалить объект «${name}»?`,
      content: 'Объект будет скрыт из системы. Связанные заявки и оборудование сохранятся.',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await api.delete(`/ServiceObjects/${id}`);
          message.success('Объект удалён');
          fetchObjects();
        } catch {
          message.error('Не удалось удалить объект');
        }
      },
    });
  };

  const columns = [
    { title: 'Наименование', dataIndex: 'name', key: 'name' },
    { title: 'Адрес', dataIndex: 'address', key: 'address' },
    { title: 'Клиент', dataIndex: 'clientName', key: 'clientName' },
    {
      title: 'Координаты',
      key: 'location',
      render: (record: any) => record.latitude ? (
        <Tag icon={<EnvironmentOutlined />} color="blue">
          {record.latitude.toFixed(4)}, {record.longitude.toFixed(4)}
        </Tag>
      ) : <Tag color="default">Нет GPS</Tag>
    },
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
        <Space size="small">
          <Button icon={<EditOutlined />} onClick={() => { setEditingObject(record); setIsModalOpen(true); }} />
          <Button icon={<DeleteOutlined />} danger onClick={() => handleDelete(record.id, record.name)} />
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Объекты обслуживания</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingObject(null); setIsModalOpen(true); }}>
          Добавить объект
        </Button>
      </div>

      <Card>
        <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Input
            placeholder="Поиск по наименованию или адресу"
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
          scroll={{ x: 650 }}
          pagination={{
            current: params.page,
            pageSize: params.pageSize,
            total: total,
            onChange: (page, pageSize) => setParams({ ...params, page, pageSize })
          }}
        />
      </Card>

      <ServiceObjectForm
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={() => { setIsModalOpen(false); fetchObjects(); }}
        initialData={editingObject}
      />
    </div>
  );
};

export default ServiceObjectsPage;
