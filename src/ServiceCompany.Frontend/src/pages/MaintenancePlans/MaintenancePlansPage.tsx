import React, { useState, useEffect } from 'react';
import {
  Table, Button, Input, Space, Tag, message, Card, Typography,
  Modal, Form, Select, DatePicker, Tooltip, Badge,
} from 'antd';
import {
  PlusOutlined, SearchOutlined, EditOutlined, DeleteOutlined,
  PoweroffOutlined, CalendarOutlined,
} from '@ant-design/icons';
import api from '../../api/client';
import dayjs from 'dayjs';

const { Title } = Typography;

const priorityLabels: Record<number, { label: string; color: string }> = {
  0: { label: 'Критический', color: 'red' },
  1: { label: 'Высокий',     color: 'volcano' },
  2: { label: 'Обычный',     color: 'gold' },
  3: { label: 'Низкий',      color: 'green' },
};

const CRON_PRESETS = [
  { label: 'Ежедневно (09:00)',          value: '0 9 * * *' },
  { label: 'Еженедельно (пн, 09:00)',    value: '0 9 * * 1' },
  { label: 'Каждые 2 недели (пн, 09:00)',value: '0 9 */14 * *' },
  { label: 'Ежемесячно (1-е число)',     value: '0 9 1 * *' },
  { label: 'Ежеквартально',             value: '0 9 1 */3 *' },
  { label: 'Раз в полгода',             value: '0 9 1 */6 *' },
  { label: 'Ежегодно (1 янв, 09:00)',   value: '0 9 1 1 *' },
];

const MaintenancePlansPage: React.FC = () => {
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [params, setParams] = useState({ page: 1, pageSize: 15, searchTerm: '', isActive: undefined as boolean | undefined });
  const [modalOpen, setModalOpen] = useState(false);
  const [editingPlan, setEditingPlan] = useState<any>(null);
  const [form] = Form.useForm();
  const [serviceObjects, setServiceObjects] = useState<any[]>([]);
  const [equipments, setEquipments] = useState<any[]>([]);
  const [engineers, setEngineers] = useState<any[]>([]);
  const [selectedObjectId, setSelectedObjectId] = useState<string | null>(null);
  const [cronCustom, setCronCustom] = useState(false);

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/maintenance-plans', { params });
      setData(res.data.items);
      setTotal(res.data.totalCount);
    } catch {
      message.error('Не удалось загрузить планы ТО');
    } finally {
      setLoading(false);
    }
  };

  const fetchLookups = async () => {
    try {
      const [objRes, engRes] = await Promise.all([
        api.get('/ServiceObjects', { params: { pageSize: 500, page: 1 } }),
        api.get('/Tickets/engineers'),
      ]);
      setServiceObjects(objRes.data.items ?? []);
      setEngineers(engRes.data);
    } catch {

    }
  };

  useEffect(() => { fetchData(); }, [params]);
  useEffect(() => { fetchLookups(); }, []);

  const fetchEquipments = async (objectId: string) => {
    try {
      const res = await api.get('/Equipments', { params: { serviceObjectId: objectId, pageSize: 200, page: 1 } });
      setEquipments(res.data.items ?? res.data);
    } catch {
      setEquipments([]);
    }
  };

  const openCreate = () => {
    setEditingPlan(null);
    form.resetFields();
    setCronCustom(false);
    setSelectedObjectId(null);
    setEquipments([]);
    setModalOpen(true);
  };

  const openEdit = (plan: any) => {
    setEditingPlan(plan);
    const preset = CRON_PRESETS.find(p => p.value === plan.cronExpression);
    setCronCustom(!preset);
    setSelectedObjectId(plan.serviceObjectId);
    fetchEquipments(plan.serviceObjectId);
    form.setFieldsValue({
      ...plan,
      startDate: dayjs(plan.startDate),
      endDate: plan.endDate ? dayjs(plan.endDate) : undefined,
      cronPreset: preset ? preset.value : '__custom__',
      cronCustomValue: !preset ? plan.cronExpression : undefined,
    });
    setModalOpen(true);
  };

  const handleSubmit = async (values: any) => {
    const cronExpression = values.cronPreset === '__custom__'
      ? values.cronCustomValue
      : values.cronPreset;

    const payload = {
      serviceObjectId:    values.serviceObjectId,
      equipmentId:        values.equipmentId ?? null,
      title:              values.title,
      description:        values.description ?? null,
      cronExpression,
      startDate:          values.startDate.toISOString(),
      endDate:            values.endDate?.toISOString() ?? null,
      defaultEngineerId:  values.defaultEngineerId ?? null,
      defaultPriority:    values.defaultPriority ?? 2,
      checklistTemplateJson: null,
    };

    try {
      if (editingPlan) {
        await api.put(`/maintenance-plans/${editingPlan.id}`, { id: editingPlan.id, ...payload });
        message.success('План ТО обновлён');
      } else {
        await api.post('/maintenance-plans', payload);
        message.success('План ТО создан');
      }
      setModalOpen(false);
      fetchData();
    } catch (err: any) {
      const data = err?.response?.data;
      const text = typeof data === 'string' ? data : data?.title ?? 'Ошибка при сохранении';
      message.error(text);
    }
  };

  const handleToggle = async (id: string, currentActive: boolean) => {
    try {
      await api.patch(`/maintenance-plans/${id}/toggle`);
      message.success(`План ${currentActive ? 'деактивирован' : 'активирован'}`);
      fetchData();
    } catch {
      message.error('Не удалось изменить статус плана');
    }
  };

  const handleDelete = (id: string, title: string) => {
    Modal.confirm({
      title: `Удалить план «${title}»?`,
      content: 'Это действие нельзя отменить.',
      okText: 'Удалить',
      okType: 'danger',
      cancelText: 'Отмена',
      onOk: async () => {
        try {
          await api.delete(`/maintenance-plans/${id}`);
          message.success('План удалён');
          fetchData();
        } catch {
          message.error('Не удалось удалить план');
        }
      },
    });
  };

  const columns = [
    {
      title: 'Название',
      dataIndex: 'title',
      key: 'title',
      render: (text: string, record: any) => (
        <Space direction="vertical" size={0}>
          <span style={{ fontWeight: 500 }}>{text}</span>
          {record.description && (
            <span style={{ fontSize: 12, color: '#888' }}>{record.description}</span>
          )}
        </Space>
      ),
    },
    {
      title: 'Объект / Оборудование',
      key: 'object',
      render: (_: any, r: any) => (
        <Space direction="vertical" size={0}>
          <span>{r.serviceObjectName}</span>
          {r.equipmentName && <span style={{ fontSize: 12, color: '#888' }}>{r.equipmentName}</span>}
        </Space>
      ),
    },
    {
      title: 'Расписание',
      dataIndex: 'cronExpression',
      key: 'cron',
      render: (cron: string) => {
        const preset = CRON_PRESETS.find(p => p.value === cron);
        return (
          <Tooltip title={`Cron: ${cron}`}>
            <Space size={4}><CalendarOutlined /><span>{preset ? preset.label : cron}</span></Space>
          </Tooltip>
        );
      },
    },
    {
      title: 'Приоритет',
      dataIndex: 'defaultPriority',
      key: 'priority',
      render: (p: number) => (
        <Tag color={priorityLabels[p]?.color}>{priorityLabels[p]?.label}</Tag>
      ),
    },
    {
      title: 'Активен',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (active: boolean) => (
        <Badge status={active ? 'success' : 'default'} text={active ? 'Да' : 'Нет'} />
      ),
    },
    {
      title: 'Дата начала',
      dataIndex: 'startDate',
      key: 'startDate',
      render: (d: string) => dayjs(d).format('DD.MM.YYYY'),
    },
    {
      title: 'Действия',
      key: 'actions',
      render: (_: any, record: any) => (
        <Space>
          <Tooltip title="Редактировать">
            <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)} />
          </Tooltip>
          <Tooltip title={record.isActive ? 'Деактивировать' : 'Активировать'}>
            <Button
              size="small"
              icon={<PoweroffOutlined />}
              onClick={() => handleToggle(record.id, record.isActive)}
              type={record.isActive ? 'default' : 'primary'}
            />
          </Tooltip>
          <Tooltip title="Удалить">
            <Button size="small" danger icon={<DeleteOutlined />} onClick={() => handleDelete(record.id, record.title)} />
          </Tooltip>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>Планы технического обслуживания</Title>
        <Button type="primary" size="large" icon={<PlusOutlined />} onClick={openCreate}>
          Создать план
        </Button>
      </div>

      <Card bodyStyle={{ paddingBottom: 0 }}>
        <div className="filter-bar" style={{ marginBottom: 16, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Input
            placeholder="Поиск по названию..."
            prefix={<SearchOutlined />}
            style={{ width: 250 }}
            onChange={(e) => setParams(p => ({ ...p, searchTerm: e.target.value, page: 1 }))}
          />
          <Select
            placeholder="Фильтр по статусу"
            style={{ width: 160, minWidth: 130 }}
            allowClear
            onChange={(val) => setParams(p => ({ ...p, isActive: val, page: 1 }))}
          >
            <Select.Option value={true}>Активные</Select.Option>
            <Select.Option value={false}>Неактивные</Select.Option>
          </Select>
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
            onChange: (page, pageSize) => setParams(p => ({ ...p, page, pageSize })),
          }}
        />
      </Card>

      <Modal
        title={editingPlan ? 'Редактировать план ТО' : 'Создать план ТО'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        okText={editingPlan ? 'Сохранить' : 'Создать'}
        cancelText="Отмена"
        width="min(640px, 95vw)"
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit}>
          <Form.Item name="title" label="Название" rules={[{ required: true, message: 'Введите название' }]}>
            <Input placeholder="Например: Ежемесячное ТО кондиционеров" />
          </Form.Item>

          <Form.Item name="description" label="Описание">
            <Input.TextArea rows={2} placeholder="Краткое описание работ..." />
          </Form.Item>

          {!editingPlan && (
            <Form.Item
              name="serviceObjectId"
              label="Объект обслуживания"
              rules={[{ required: true, message: 'Выберите объект' }]}
            >
              <Select
                showSearch
                placeholder="Выберите объект"
                optionFilterProp="children"
                onChange={(val) => {
                  setSelectedObjectId(val);
                  form.setFieldValue('equipmentId', undefined);
                  if (val) fetchEquipments(val);
                  else setEquipments([]);
                }}
              >
                {serviceObjects.map((o: any) => (
                  <Select.Option key={o.id} value={o.id}>{o.name}</Select.Option>
                ))}
              </Select>
            </Form.Item>
          )}

          {!editingPlan && (
            <Form.Item name="equipmentId" label="Оборудование (необязательно)">
              <Select
                showSearch
                placeholder="Выберите оборудование"
                optionFilterProp="children"
                allowClear
                disabled={!selectedObjectId}
              >
                {equipments.map((e: any) => (
                  <Select.Option key={e.id} value={e.id}>{e.name}</Select.Option>
                ))}
              </Select>
            </Form.Item>
          )}

          <Form.Item
            name="cronPreset"
            label="Периодичность"
            rules={[{ required: true, message: 'Укажите периодичность' }]}
            initialValue={CRON_PRESETS[0].value}
          >
            <Select
              onChange={(val) => setCronCustom(val === '__custom__')}
            >
              {CRON_PRESETS.map(p => (
                <Select.Option key={p.value} value={p.value}>{p.label}</Select.Option>
              ))}
              <Select.Option value="__custom__">Своё расписание (Cron)</Select.Option>
            </Select>
          </Form.Item>

          {cronCustom && (
            <Form.Item
              name="cronCustomValue"
              label="Cron-выражение"
              rules={[{ required: true, message: 'Введите cron-выражение' }]}
            >
              <Input placeholder="Например: 0 9 * * 1" />
            </Form.Item>
          )}

          <Form.Item
            name="startDate"
            label="Дата начала"
            rules={[{ required: true, message: 'Укажите дату начала' }]}
            initialValue={dayjs()}
          >
            <DatePicker style={{ width: '100%' }} format="DD.MM.YYYY" />
          </Form.Item>

          <Form.Item name="endDate" label="Дата окончания (необязательно)">
            <DatePicker style={{ width: '100%' }} format="DD.MM.YYYY" />
          </Form.Item>

          <Form.Item
            name="defaultPriority"
            label="Приоритет заявок по умолчанию"
            initialValue={2}
          >
            <Select>
              {Object.entries(priorityLabels).map(([k, v]) => (
                <Select.Option key={k} value={parseInt(k)}>{v.label}</Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item name="defaultEngineerId" label="Инженер по умолчанию">
            <Select showSearch placeholder="Не назначен" allowClear optionFilterProp="children">
              {engineers.map((e: any) => (
                <Select.Option key={e.id} value={e.id}>{e.fullName}</Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default MaintenancePlansPage;
