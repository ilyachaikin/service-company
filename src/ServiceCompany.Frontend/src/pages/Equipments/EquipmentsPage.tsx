import React, { useState, useEffect } from 'react';
import { Table, Button, Input, Space, Tag, message, Card, Typography, Modal } from 'antd';
import { PlusOutlined, SearchOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import api from '../../api/client';
import EquipmentForm from './EquipmentForm';

const { Title } = Typography;

const EquipmentsPage: React.FC = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState({ page: 1, pageSize: 10, searchTerm: '', serviceObjectId: undefined });
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEquipment, setEditingEquipment] = useState<any>(null);

  const fetchEquipment = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Equipments', { params });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch (err) {
      message.error('Не удалось загрузить оборудование');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchEquipment(); }, [params]);

  const handleDelete = (id: string, name: string) => {
    Modal.confirm({
      title: `Удалить оборудование «${name}»?`,
      content: 'Оборудование будет помечено как удалённое. История заявок сохранится.',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await api.delete(`/Equipments/${id}`);
          message.success('Оборудование удалено');
          fetchEquipment();
        } catch {
          message.error('Не удалось удалить оборудование');
        }
      },
    });
  };

  const columns = [
    { title: 'Наименование', dataIndex: 'name', key: 'name' },
    { title: 'Серийный №', dataIndex: 'serialNumber', key: 'serialNumber' },
    { title: 'Модель', dataIndex: 'model', key: 'model' },
    { title: 'Объект', dataIndex: 'serviceObjectName', key: 'serviceObjectName' },
    { title: 'Клиент', dataIndex: 'clientName', key: 'clientName' },
    {
      title: 'Статус',
      dataIndex: 'status',
      key: 'status',
      render: (status: number) => {
        const statusMap: any = {
          0: { label: 'Работает', color: 'green' },
          1: { label: 'Неисправно', color: 'red' },
          2: { label: 'На обслуживании', color: 'blue' },
          3: { label: 'Списано', color: 'default' }
        };
        const s = statusMap[status] || { label: 'Неизвестно', color: 'default' };
        return <Tag color={s.color}>{s.label}</Tag>;
      }
    },
    {
      title: 'Действия',
      key: 'action',
      render: (_: any, record: any) => (
        <Space size="small">
          <Button icon={<EditOutlined />} onClick={() => { setEditingEquipment(record); setIsModalOpen(true); }} />
          <Button icon={<DeleteOutlined />} danger onClick={() => handleDelete(record.id, record.name)} />
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Оборудование</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingEquipment(null); setIsModalOpen(true); }}>
          Добавить оборудование
        </Button>
      </div>

      <Card>
        <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Input
            placeholder="Поиск по наименованию или серийному номеру"
            prefix={<SearchOutlined />}
            style={{ width: 340, minWidth: 180 }}
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

      <EquipmentForm
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={() => { setIsModalOpen(false); fetchEquipment(); }}
        initialData={editingEquipment}
      />
    </div>
  );
};

export default EquipmentsPage;
