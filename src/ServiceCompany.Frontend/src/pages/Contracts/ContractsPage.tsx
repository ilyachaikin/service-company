import React, { useState, useEffect } from 'react';
import { Table, Button, Input, Tag, Space, Card, Typography, message, Modal, Form, Select, DatePicker, InputNumber } from 'antd';
import { PlusOutlined, SearchOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import api from '../../api/client';
import dayjs from 'dayjs';

const { Title } = Typography;

const statusColors: any = {
  0: { color: 'default', label: 'Черновик' },
  1: { color: 'success', label: 'Активен' },
  2: { color: 'warning', label: 'Приостановлен' },
  3: { color: 'error', label: 'Истёк' },
  4: { color: 'default', label: 'Расторгнут' }
};

const ContractsPage: React.FC = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState({ page: 1, pageSize: 15, searchTerm: '' });
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [clients, setClients] = useState([]);
  const [slaPolicies, setSlaPolicies] = useState([]);
  const [editingContract, setEditingContract] = useState<any>(null);
  const [form] = Form.useForm();

  const fetchContracts = async () => {
    setLoading(true);
    try {
      const res = await api.get('/Contracts', { params });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch { message.error('Не удалось загрузить договоры'); }
    finally { setLoading(false); }
  };

  const fetchLookups = async () => {
    const [clientsRes, slaRes] = await Promise.all([
      api.get('/Clients', { params: { pageSize: 200 } }),
      api.get('/SlaPolicies')
    ]);
    setClients(clientsRes.data.items);
    setSlaPolicies(slaRes.data);
  };

  useEffect(() => { fetchContracts(); }, [params]);
  useEffect(() => { fetchLookups(); }, []);

  const handleSave = async (values: any) => {
    try {
      const payload = {
        ...values,
        startDate: values.dates[0].toISOString(),
        endDate: values.dates[1].toISOString(),
      };
      delete payload.dates;

      if (editingContract) {
        await api.put(`/Contracts/${editingContract.id}`, { ...payload, id: editingContract.id });
      } else {
        await api.post('/Contracts', payload);
      }
      message.success(editingContract ? 'Договор обновлён' : 'Договор создан');
      setIsModalOpen(false);
      form.resetFields();
      setEditingContract(null);
      fetchContracts();
    } catch (err: any) {
      message.error(err.response?.data?.error || 'Ошибка сохранения');
    }
  };

  const openCreate = () => {
    setEditingContract(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const handleDelete = (id: string, number: string) => {
    Modal.confirm({
      title: `Удалить договор «${number}»?`,
      content: 'Договор будет помечен как удалённый.',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await api.delete(`/Contracts/${id}`);
          message.success('Договор удалён');
          fetchContracts();
        } catch {
          message.error('Не удалось удалить договор');
        }
      },
    });
  };

  const openEdit = (record: any) => {
    setEditingContract(record);
    form.setFieldsValue({
      number: record.number,
      clientId: record.clientId,
      slaPolicyId: record.slaPolicyId,
      totalAmount: record.totalAmount,
      status: record.status,
      dates: [dayjs(record.startDate), dayjs(record.endDate)]
    });
    setIsModalOpen(true);
  };

  const columns = [
    { title: 'Номер', dataIndex: 'number', key: 'number', render: (t: string) => <b>{t}</b> },
    { title: 'Клиент', dataIndex: 'clientName', key: 'clientName' },
    { title: 'SLA', dataIndex: 'slaPolicyName', key: 'slaPolicyName', render: (t: string) => <Tag color="blue">{t}</Tag> },
    { title: 'Дата начала', dataIndex: 'startDate', key: 'startDate', render: (d: string) => dayjs(d).format('DD.MM.YYYY') },
    { title: 'Дата окончания', dataIndex: 'endDate', key: 'endDate', render: (d: string) => dayjs(d).format('DD.MM.YYYY') },
    { title: 'Сумма', dataIndex: 'totalAmount', key: 'totalAmount', render: (a: number) => `${a?.toLocaleString()} ₸` },
    { title: 'Статус', dataIndex: 'status', key: 'status', render: (s: number) => <Tag color={statusColors[s]?.color}>{statusColors[s]?.label}</Tag> },
    {
      title: 'Действия', key: 'actions',
      render: (_: any, record: any) => (
        <Space size="small">
          <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(record)} />
          <Button type="text" danger icon={<DeleteOutlined />} onClick={() => handleDelete(record.id, record.number)} />
        </Space>
      )
    }
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Договоры</Title>
        <Button type="primary" size="large" icon={<PlusOutlined />} onClick={openCreate}>Новый договор</Button>
      </div>

      <Card>
        <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Input
            placeholder="Поиск по номеру или клиенту..."
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
          scroll={{ x: 800 }}
          pagination={{
            current: params.page,
            pageSize: params.pageSize,
            total,
            onChange: (page, pageSize) => setParams({ ...params, page, pageSize })
          }}
        />
      </Card>

      <Modal
        title={editingContract ? 'Редактировать договор' : 'Новый договор'}
        open={isModalOpen}
        onCancel={() => { setIsModalOpen(false); setEditingContract(null); form.resetFields(); }}
        onOk={() => form.submit()}
        okText={editingContract ? 'Сохранить' : 'Создать'}
        cancelText="Отмена"
        width="min(600px, 95vw)"
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item name="number" label="Номер договора" rules={[{ required: true, message: 'Введите номер договора' }]}>
            <Input placeholder="Например: ДГ-2026-001" />
          </Form.Item>
          <Form.Item name="clientId" label="Клиент" rules={[{ required: true, message: 'Выберите клиента' }]}>
            <Select placeholder="Выберите клиента" showSearch optionFilterProp="children" disabled={!!editingContract}>
              {clients.map((c: any) => <Select.Option key={c.id} value={c.id}>{c.name}</Select.Option>)}
            </Select>
          </Form.Item>
          <Form.Item name="slaPolicyId" label="SLA-политика" rules={[{ required: true, message: 'Выберите SLA-политику' }]}>
            <Select placeholder="Выберите SLA-политику">
              {slaPolicies.map((s: any) => (
                <Select.Option key={s.id} value={s.id}>
                  {s.name} (Ответ: {s.responseTimeHours}ч / Решение: {s.resolutionTimeHours}ч)
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="dates" label="Период действия договора" rules={[{ required: true, message: 'Укажите период' }]}>
            <DatePicker.RangePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="totalAmount" label="Сумма договора (₸)" rules={[{ required: true, message: 'Введите сумму' }]}>
            <InputNumber style={{ width: '100%' }} min={0} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
          </Form.Item>
          {editingContract && (
            <Form.Item name="status" label="Статус">
              <Select>
                {Object.entries(statusColors).map(([key, val]: any) => (
                  <Select.Option key={key} value={parseInt(key)}>{val.label}</Select.Option>
                ))}
              </Select>
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
};

export default ContractsPage;
